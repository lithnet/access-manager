using System;
using System.DirectoryServices;
using System.Security.Principal;

namespace Lithnet.AccessManager
{
    public sealed class ActiveDirectoryUser : IUser
    {
        private readonly DirectoryEntry de;

        internal static string[] PropertiesToGet = { "samAccountName", "distinguishedName", "description", "displayName", "userPrincipalName", "objectSid", "mail", "givenName", "sn", "msDS-PrincipalName", "objectClass" };

        public ActiveDirectoryUser(DirectoryEntry directoryEntry)
        {
            directoryEntry.ThrowIfNotObjectClass("user");
            this.de = directoryEntry;
            this.de.RefreshCache(PropertiesToGet);
        }

        public string Path => this.de.Path;

        public string SamAccountName => this.de.GetPropertyString("samAccountName");

        public string MsDsPrincipalName => this.de.GetPropertyString("msDS-PrincipalName");

        public string DistinguishedName => this.de.GetPropertyString("distinguishedName");

        public SecurityIdentifier Sid => this.de.GetPropertySid("objectSid");

        public string DisplayName => this.de.GetPropertyString("displayName");

        public string UserPrincipalName => this.de.GetPropertyString("userPrincipalName");

        public string Description => this.de.GetPropertyString("description");

        public string EmailAddress => this.de.GetPropertyString("mail");

        public Guid? Guid => this.de.GetPropertyGuid("objectGuid");

        public string GivenName => this.de.GetPropertyString("givenName");

        public string Surname => this.de.GetPropertyString("sn");
    }
}