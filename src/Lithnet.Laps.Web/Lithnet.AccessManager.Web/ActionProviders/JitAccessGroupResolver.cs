using System;

namespace Lithnet.AccessManager.Web
{
    public class JitAccessGroupResolver : IJitAccessGroupResolver
    {
        private readonly IDirectory directory;

        public JitAccessGroupResolver(IDirectory directory)
        {
            this.directory = directory;
        }

        public IGroup GetJitAccessGroup(IComputer computer, string groupName)
        {
            string authorizingGroupName = groupName;

            if (authorizingGroupName != null)
            {
                if (authorizingGroupName.StartsWith("prop:", StringComparison.OrdinalIgnoreCase))
                {
                    string propertyName = authorizingGroupName.Remove(0, "prop:".Length);
                    var computerEntry = this.directory.GetDirectoryEntry(computer, propertyName);

                    var referredGroup = computerEntry.GetPropertyString(propertyName);

                    if (referredGroup == null)
                    {
                        throw new ObjectNotFoundException($"The JIT group was not found on the computer property {propertyName}");
                    }

                    return this.directory.GetGroup(referredGroup);
                }
                else
                {
                    authorizingGroupName = authorizingGroupName.Replace("{computerName}", computer.SamAccountName.TrimEnd('$'), StringComparison.OrdinalIgnoreCase);
                    return this.directory.GetGroup(authorizingGroupName);
                }
            }

            if (!this.directory.TryGetLamSettings(computer, out ILamSettings lamSettings))
            {
                throw new ObjectNotFoundException($"The Lithnet Access  Manager object for computer {computer.MsDsPrincipalName} was not found in the directory");
            }

            if (lamSettings.JitGroupReference == null)
            {
                throw new ObjectNotFoundException($"The Lithnet Access Manager object for computer {computer.MsDsPrincipalName} was found, but it did not contain a group entry");
            }

            return this.directory.GetGroup(lamSettings.JitGroupReference);
        }
    }
}
