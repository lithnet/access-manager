using System;
using System.DirectoryServices;
using System.Security.Principal;

namespace Lithnet.AccessManager
{
    public sealed class ActiveDirectoryGroupManagedServiceAccount : IGroupManagedServiceAccount
    {
        private readonly DirectoryEntry de;

        internal static string[] PropertiesToGet = { "samAccountName", "distinguishedName", "description", "displayName", "objectSid", "givenName", "sn", "msDS-PrincipalName", "objectClass" };

        public ActiveDirectoryGroupManagedServiceAccount(DirectoryEntry directoryEntry)
        {
            directoryEntry.ThrowIfNotObjectClass("msDS-GroupManagedServiceAccount");
            this.de = directoryEntry;
            this.de.RefreshCache(PropertiesToGet);
        }

        public string Path => this.de.Path;

        public string SamAccountName => this.de.GetPropertyString("samAccountName");

        public string MsDsPrincipalName => this.de.GetPropertyString("msDS-PrincipalName");

        public string DistinguishedName => this.de.GetPropertyString("distinguishedName");

        public SecurityIdentifier Sid => this.de.GetPropertySid("objectSid");

        public string DisplayName => this.de.GetPropertyString("displayName");

        public string Description => this.de.GetPropertyString("description");

        public Guid? Guid => this.de.GetPropertyGuid("objectGuid");

        public string GivenName => this.de.GetPropertyString("givenName");

        public string Surname => this.de.GetPropertyString("sn");

        public string Type => "GroupManagedServiceAccount";
    }
}