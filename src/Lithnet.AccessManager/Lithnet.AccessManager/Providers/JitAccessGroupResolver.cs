using System;
using System.Security.Principal;

namespace Lithnet.AccessManager
{
    public class JitAccessGroupResolver : IJitAccessGroupResolver
    {
        private readonly IActiveDirectory directory;
        private readonly IDiscoveryServices discoveryServices;

        public JitAccessGroupResolver(IActiveDirectory directory, IDiscoveryServices discoveryServices)
        {
            this.directory = directory;
            this.discoveryServices = discoveryServices;
        }

        public IActiveDirectoryGroup GetJitGroup(IComputer computer, string groupName)
        {
            if (computer is IActiveDirectoryComputer adComputer)
            {
                return this.GetJitGroup(adComputer, groupName);
            }

            throw new InvalidOperationException("The computer object type supplied is not known");
        }

        public IActiveDirectoryGroup GetJitGroup(IActiveDirectoryComputer computer, string groupName)
        {
            string authorizingGroupName = groupName;

            if (authorizingGroupName == null)
            {
                throw new ConfigurationException("There was no JIT group name provided");
            }

            if (authorizingGroupName.StartsWith("prop:", StringComparison.OrdinalIgnoreCase))
            {
                string propertyName = authorizingGroupName.Remove(0, "prop:".Length);
                var referredGroup = computer.DirectoryEntry.GetPropertyString(propertyName);

                if (referredGroup == null)
                {
                    throw new ObjectNotFoundException(
                        $"The JIT group was not found on the computer property {propertyName}");
                }

                return this.directory.GetGroup(referredGroup);
            }
            else
            {
                string domain = this.discoveryServices.GetDomainNameNetBios(computer.Sid);
                string computerName = computer.SamAccountName.TrimEnd('$');

                return this.GetJitGroup(authorizingGroupName, computerName, domain);
            }
        }

        public IActiveDirectoryGroup GetJitGroup(string groupNameTemplate, string computerName, string domain)
        {
            if (groupNameTemplate == null)
            {
                throw new ConfigurationException("There was no JIT group name provided");
            }

            if (groupNameTemplate.TryParseAsSid(out SecurityIdentifier sid))
            {
                return this.directory.GetGroup(sid);
            }

            groupNameTemplate = this.BuildGroupName(groupNameTemplate, domain, computerName);

            if (this.directory.TryGetGroup(groupNameTemplate, out IActiveDirectoryGroup group))
            {
                return group;
            }
            else
            {
                throw new ObjectNotFoundException($"The JIT group could not be found: {groupNameTemplate}");
            }
        }

        public string BuildGroupName(string groupNameTemplate, string computerDomain, string computerName)
        {
            if (groupNameTemplate == null)
            {
                return null;
            }

#if NETCOREAPP
            groupNameTemplate = groupNameTemplate
                .Replace("{computerName}", computerName, StringComparison.OrdinalIgnoreCase)
                .Replace("%computerName%", computerName, StringComparison.OrdinalIgnoreCase)
                .Replace("{domain}", computerDomain, StringComparison.OrdinalIgnoreCase)
                .Replace("%domain%", computerName, StringComparison.OrdinalIgnoreCase)
                .Replace("{computerDomain}", computerDomain, StringComparison.OrdinalIgnoreCase)
                .Replace("%computerDomain%", computerDomain, StringComparison.OrdinalIgnoreCase);
#else
            groupNameTemplate = groupNameTemplate.ToLowerInvariant()
                .Replace("{computername}", computerName)
                .Replace("%computername%", computerName)
                .Replace("{domain}", computerDomain)
                .Replace("%domain%", computerDomain)
                .Replace("{computerdomain}", computerDomain)
                .Replace("%computerdomain%", computerDomain);
#endif

            if (!groupNameTemplate.Contains("\\"))
            {
                groupNameTemplate = $"{computerDomain}\\{groupNameTemplate}";
            }

            return groupNameTemplate;
        }

        public bool IsTemplatedName(string groupNameTemplate)
        {
            return groupNameTemplate.IndexOf("{computerName}", StringComparison.OrdinalIgnoreCase) > -1
                || groupNameTemplate.IndexOf("%computerName%", StringComparison.OrdinalIgnoreCase) > -1
                ;
        }
    }
}
