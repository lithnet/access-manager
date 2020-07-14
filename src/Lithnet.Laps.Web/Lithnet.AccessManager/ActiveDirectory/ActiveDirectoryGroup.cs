using System;
using System.DirectoryServices;
using System.Security.Principal;

namespace Lithnet.AccessManager
{
    public sealed class ActiveDirectoryGroup: IGroup
    {
        private readonly SearchResult group;

        private readonly DirectoryEntry de;

        internal static string[] PropertiesToGet = new string[] { "samAccountName", "distinguishedName", "tokenGroups", "displayName", "objectGuid", "objectSid", "samAccountName", "msDS-PrincipalName", "objectClass" };

        public ActiveDirectoryGroup(SearchResult groupPrincipal)
        {
            this.group = groupPrincipal;
        }

        public ActiveDirectoryGroup (DirectoryEntry directoryEntry)
        {
            this.de = directoryEntry;
        }

        public Guid? Guid => this.group?.GetPropertyGuid("objectGuid") ?? this.de.GetPropertyGuid("objectGuid");

        public string MsDsPrincipalName => this.group?.GetPropertyString("msDS-PrincipalName") ?? this.de.GetPropertyString("msDS-PrincipalName");

        public SecurityIdentifier Sid => this.group?.GetPropertySid("objectSid") ?? this.de.GetPropertySid("objectSid");

        public string SamAccountName => this.group?.GetPropertyString("samAccountName") ?? this.de.GetPropertyString("samAccountName");

        public string DistinguishedName => this.group?.GetPropertyString("distinguishedName") ?? this.de.GetPropertyString("distinguishedName");
    }
}
