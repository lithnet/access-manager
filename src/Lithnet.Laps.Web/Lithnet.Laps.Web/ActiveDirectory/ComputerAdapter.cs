using System;
using System.DirectoryServices;
using System.Security.Principal;
using Lithnet.Laps.Web.Models;

namespace Lithnet.Laps.Web.ActiveDirectory
{
    public sealed class ComputerAdapter : IComputer
    {
        internal static string[] PropertiesToGet = new string[] { "samAccountName", "distinguishedName", "description", "displayName", "objectGuid", "objectSid" };

        private readonly SearchResult computer;

        public ComputerAdapter(SearchResult computer)
        {
            this.computer = computer;
        }

        public string SamAccountName => this.computer.GetPropertyString("samAccountName");

        public string DistinguishedName => this.computer.GetPropertyString("distinguishedName");

        public string Description => this.computer.GetPropertyString("description");

        public string DisplayName => this.computer.GetPropertyString("displayName");

        public Guid? Guid => this.computer.GetPropertyGuid("objectGuid");

        public SecurityIdentifier Sid => this.computer.GetPropertySid("objectSid");
    }
}