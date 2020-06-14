using System;
using System.DirectoryServices;
using System.Security.Principal;
using Lithnet.Laps.Web.Internal;

namespace Lithnet.Laps.Web.ActiveDirectory
{
    public sealed class ActiveDirectoryGroup: IGroup
    {
        private readonly SearchResult group;

        internal static string[] PropertiesToGet = new string[] { "samAccountName", "distinguishedName", "tokenGroups", "displayName", "objectGuid", "objectSid", "samAccountName", "msDS-PrincipalName" };

        public ActiveDirectoryGroup(SearchResult groupPrincipal)
        {
            this.group = groupPrincipal;
        }

        public Guid? Guid => this.group.GetPropertyGuid("objectGuid");

        public string MsDsPrincipalName => this.group.GetPropertyString("msDS-PrincipalName");

        public SecurityIdentifier Sid => this.group.GetPropertySid("objectSid");

        public string SamAccountName => this.group.GetPropertyString("samAccountName");

        public string DistinguishedName => this.group.GetPropertyString("distinguishedName");
    }
}
