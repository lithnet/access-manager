using System;
using System.Security.Cryptography;
using System.Security.Principal;

namespace Lithnet.AccessManager
{
    public class JitAccessGroupResolver : IJitAccessGroupResolver
    {
        private readonly IDirectory directory;

        public JitAccessGroupResolver(IDirectory directory)
        {
            this.directory = directory;
        }

        public IGroup GetJitGroup(IComputer computer, string groupName)
        {
            string authorizingGroupName = groupName;

            if (authorizingGroupName == null)
            {
                throw new ConfigurationException("There was no JIT group name provided");
            }

            if (authorizingGroupName.StartsWith("prop:", StringComparison.OrdinalIgnoreCase))
            {
                string propertyName = authorizingGroupName.Remove(0, "prop:".Length);
                var computerEntry = this.directory.GetDirectoryEntry(computer, propertyName);

                var referredGroup = computerEntry.GetPropertyString(propertyName);

                if (referredGroup == null)
                {
                    throw new ObjectNotFoundException(
                        $"The JIT group was not found on the computer property {propertyName}");
                }

                return this.directory.GetGroup(referredGroup);
            }
            else
            {
                string domain = this.directory.GetDomainNetbiosName(computer.Sid);
                string computerName = computer.SamAccountName.TrimEnd('$');

                return this.GetJitGroup(authorizingGroupName, computerName, domain);
            }
        }

        public IGroup GetJitGroup(string groupNameTemplate, string computerName, string domain)
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

            if (this.directory.TryGetGroup(groupNameTemplate, out IGroup group))
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

            groupNameTemplate = groupNameTemplate
                .Replace("{computerName}", computerName, StringComparison.OrdinalIgnoreCase)
                .Replace("{domain}", computerDomain, StringComparison.OrdinalIgnoreCase)
                .Replace("{computerDomain}", computerDomain, StringComparison.OrdinalIgnoreCase);

            if (!groupNameTemplate.Contains('\\'))
            {
                groupNameTemplate = $"{computerDomain}\\{groupNameTemplate}";
            }

            return groupNameTemplate;
        }

        public bool IsTemplatedName(string groupNameTemplate)
        {
            return groupNameTemplate.IndexOf("{computerName}", StringComparison.OrdinalIgnoreCase) > -1;
        }
    }
}
