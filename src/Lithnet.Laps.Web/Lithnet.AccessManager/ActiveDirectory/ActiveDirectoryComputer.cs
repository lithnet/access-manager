using System;
using System.DirectoryServices;
using System.Security.Principal;

namespace Lithnet.AccessManager
{
    public sealed class ActiveDirectoryComputer : IComputer
    {
        internal static string[] PropertiesToGet = new string[] { "samAccountName", "distinguishedName", "description", "displayName", "objectGuid", "objectSid" , "msDS-PrincipalName", "objectClass" };

        private readonly SearchResult computer;

        public ActiveDirectoryComputer(SearchResult computer)
        {
            this.computer = computer;
        }

        public string SamAccountName => this.computer.GetPropertyString("samAccountName");

        public string DistinguishedName => this.computer.GetPropertyString("distinguishedName");

        public string MsDsPrincipalName => this.computer.GetPropertyString("msDS-PrincipalName");

        public string Description => this.computer.GetPropertyString("description");

        public string DisplayName => this.computer.GetPropertyString("displayName");

        public Guid? Guid => this.computer.GetPropertyGuid("objectGuid");

        public SecurityIdentifier Sid => this.computer.GetPropertySid("objectSid");
    }
}