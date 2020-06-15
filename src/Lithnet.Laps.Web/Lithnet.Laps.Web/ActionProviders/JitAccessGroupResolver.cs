using Lithnet.Laps.Web.ActiveDirectory;
using Lithnet.Laps.Web.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lithnet.Laps.Web.Internal;

namespace Lithnet.Laps.Web.ActionProviders
{
    public class JitAccessGroupResolver : IJitAccessGroupResolver
    {
        private const string MsDsAppConfiguration = "msDs-App-Configuration";
        private const string MsDsObjectReference = "msDS-ObjectReference";
        private const string ApplicationName = "Lithnet Access Manager";

        private readonly IDirectory directory;

        public JitAccessGroupResolver(IDirectory directory)
        {
            this.directory = directory;
        }

        public IGroup GetJitAccessGroup(IComputer computer, IJsonTarget target)
        {
            string authorizingGroupName = target.Jit?.AuthorizingGroup;

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

            string filter = $"(&(objectClass={MsDsAppConfiguration})(applicationName={ApplicationName}))";
            var appData = this.directory.SearchDirectoryEntry(computer.DistinguishedName, filter, System.DirectoryServices.SearchScope.OneLevel, MsDsObjectReference);

            if (appData == null)
            {
                throw new ObjectNotFoundException($"The JIT access object for computer {computer.MsDsPrincipalName} was not found");
            }

            string groupRef = appData.GetPropertyString(MsDsObjectReference);

            if (groupRef == null)
            {
                throw new ObjectNotFoundException($"The JIT access object for computer {computer.MsDsPrincipalName} did not contain a group");
            }

            return this.directory.GetGroup(groupRef);
        }
    }
}
