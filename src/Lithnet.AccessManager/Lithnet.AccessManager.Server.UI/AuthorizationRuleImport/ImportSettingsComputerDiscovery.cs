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

        public bool FilterNonAccountSids { get; set; } = true;

        public bool FilterDisabledComputers { get; set; }

        public bool ExcludeConflictObjects { get; set; } = true;

        public string ImportOU { get; set; }

        public HashSet<SecurityIdentifier> ComputerFilter { get; } = new HashSet<SecurityIdentifier>();
    }
}
