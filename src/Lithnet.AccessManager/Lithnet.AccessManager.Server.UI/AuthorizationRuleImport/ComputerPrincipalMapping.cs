using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;
using System.Security.Principal;

namespace Lithnet.AccessManager.Server.UI.AuthorizationRuleImport
{
    public class ComputerPrincipalMapping
    {
        private HashSet<SecurityIdentifier> uniqueAdmins;

        private readonly SearchResult computer;

        internal ComputerPrincipalMapping(SearchResult computer, OUPrincipalMapping parent)
        {
            this.Parent = parent;
            this.computer = computer;
        }

        public string SamAccountName => this.computer.GetPropertyString("samAccountName");
        
        public string PrincipalName => this.computer.GetPropertyString("msDS-PrincipalName");

        public SecurityIdentifier Sid => this.computer.GetPropertySid("objectSid");

        public OUPrincipalMapping Parent { get; }

        public bool HasError { get; internal set; }

        public bool IsMissing { get; internal set; }

        public Exception Exception { get; internal set; }

        public HashSet<SecurityIdentifier> Principals { get; } = new HashSet<SecurityIdentifier>();

        public List<DiscoveryError> DiscoveryErrors { get; } = new List<DiscoveryError>();

        public HashSet<SecurityIdentifier> PrincipalsUniqueToThisLevel
        {
            get
            {
                if (this.uniqueAdmins == null)
                {
                    this.uniqueAdmins = this.Principals?.Except(this.Parent.CommonPrincipalsFromDescendants)?.ToHashSet() ?? new HashSet<SecurityIdentifier>();
                }

                return this.uniqueAdmins;
            }
        }
    }
}