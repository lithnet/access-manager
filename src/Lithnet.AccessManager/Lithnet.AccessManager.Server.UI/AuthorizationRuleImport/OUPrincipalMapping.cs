using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;
using System.Security.Principal;

namespace Lithnet.AccessManager.Server.UI.AuthorizationRuleImport
{
    public class OUPrincipalMapping
    {
        private HashSet<SecurityIdentifier> commonAdmins;
        private HashSet<SecurityIdentifier> thisLevelAdmins;
        private List<HashSet<SecurityIdentifier>> childAdmins;

        public List<ComputerPrincipalMapping> Computers { get; } = new List<ComputerPrincipalMapping>();

        public List<OUPrincipalMapping> DescendantOUs { get; } = new List<OUPrincipalMapping>();

        public string OUName { get; }

        public string AdsPath { get; }

        public OUPrincipalMapping Parent { get; }

        internal OUPrincipalMapping(SearchResult result, OUPrincipalMapping parent)
        {
            this.AdsPath = result.Path;
            this.OUName = result.GetPropertyString("distinguishedName");
            this.Parent = parent;
        }

        internal OUPrincipalMapping(string adsPath, string ouName)
        {
            this.AdsPath = adsPath;
            this.OUName = ouName;
        }

        public HashSet<SecurityIdentifier> CommonPrincipalsFromDescendants
        {
            get
            {
                if (this.commonAdmins == null)
                {
                    this.commonAdmins = ConsolidatedPrincipalsFromDescendants.FirstOrDefault()?.Distinct().Where(option => ConsolidatedPrincipalsFromDescendants.Skip(1).All(l => l.Contains(option))).ToHashSet() ?? new HashSet<SecurityIdentifier>();
                }

                return this.commonAdmins;
            }
        }

        public HashSet<SecurityIdentifier> PrincipalsUniqueToThisLevel
        {
            get
            {
                if (this.thisLevelAdmins == null)
                {
                    if (this.Parent?.CommonPrincipalsFromDescendants == null)
                    {
                        this.thisLevelAdmins = this.CommonPrincipalsFromDescendants ?? new HashSet<SecurityIdentifier>();
                    }
                    else
                    {
                        this.thisLevelAdmins = this.CommonPrincipalsFromDescendants?.Except(this.Parent.CommonPrincipalsFromDescendants).ToHashSet() ?? new HashSet<SecurityIdentifier>();
                    }
                }

                return this.thisLevelAdmins;
            }
        }

        public bool HasDescendantsWithErrors
        {
            get
            {
                return this.Computers.Any(t => t.HasError) || this.DescendantOUs.Any(t => t.HasDescendantsWithErrors);
            }
        }

        private List<HashSet<SecurityIdentifier>> ConsolidatedPrincipalsFromDescendants
        {
            get
            {
                if (this.childAdmins == null)
                {
                    this.childAdmins = new List<HashSet<SecurityIdentifier>>();

                    foreach (var computer in this.Computers)
                    {
                        if (!computer.HasError)
                        {
                            this.childAdmins.Add(computer.Principals);
                        }
                    }

                    foreach (var ou in this.DescendantOUs)
                    {
                        foreach (var list in ou.ConsolidatedPrincipalsFromDescendants)
                        {
                            this.childAdmins.Add(list);
                        }
                    }
                }

                return this.childAdmins;
            }
        }
    }
}