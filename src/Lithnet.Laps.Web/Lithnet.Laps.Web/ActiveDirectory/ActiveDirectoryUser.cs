using System;
using System.DirectoryServices;
using System.Security.Principal;
using Lithnet.Laps.Web.Internal;

namespace Lithnet.Laps.Web.ActiveDirectory
{
    public sealed class ActiveDirectoryUser : IUser
    {
        private readonly SearchResult user;

        internal static string[] PropertiesToGet = { "samAccountName", "distinguishedName", "description", "displayName", "userPrincipalName", "objectSid", "mail", "givenName", "sn", "msDS-PrincipalName" };

        public ActiveDirectoryUser(SearchResult user)
        {
            this.user = user;
        }

        public string SamAccountName => this.user.GetPropertyString("samAccountName");

        public string MsDsPrincipalName => this.user.GetPropertyString("msDS-PrincipalName");

        public string DistinguishedName => this.user.GetPropertyString("distinguishedName");

        public SecurityIdentifier Sid => this.user.GetPropertySid("objectSid");

        public string DisplayName => this.user.GetPropertyString("displayName");

        public string UserPrincipalName => this.user.GetPropertyString("userPrincipalName");

        public string Description => this.user.GetPropertyString("description");

        public string EmailAddress => this.user.GetPropertyString("mail");

        public Guid? Guid => this.user.GetPropertyGuid("objectGuid");

        public string GivenName => this.user.GetPropertyString("givenName");

        public string Surname => this.user.GetPropertyString("sn");
    }
}