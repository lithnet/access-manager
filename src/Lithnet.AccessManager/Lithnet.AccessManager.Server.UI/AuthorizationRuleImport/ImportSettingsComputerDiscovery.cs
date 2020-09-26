using System.Collections.Generic;
using System.Security.Principal;
using System.Text.RegularExpressions;

namespace Lithnet.AccessManager.Server.UI.AuthorizationRuleImport
{
    public abstract class ImportSettingsComputerDiscovery : ImportSettings
    {
        public bool DoNotConsolidate { get; set; }

        public bool DoNotConsolidateOnError { get; set; }

        public bool FilterLocalAccounts { get; set; } = true;
        public bool FilterUnresolvablePrincipals { get; set; } = true;

        public List<Regex> PrincipalFilters { get; } = new List<Regex>();

        public List<Regex> ComputerFilters { get; } = new List<Regex>();

        public HashSet<SecurityIdentifier> PrincipalSidFilter { get; } = new HashSet<SecurityIdentifier>();

        public string ImportOU { get; set; }
    }
}
