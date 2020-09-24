using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Lithnet.AccessManager
{
    public class AuthorizationRuleImportSettings
    {
        public bool DoNotConsolidate { get; set; }

        public bool DoNotConsolidateOnError { get; set; }

        public bool FilterLocalAccounts { get; set; } = true;

        public bool FilterUnresolvablePrincipals { get; set; } = true;

        public string ImportFile { get; set; }

        public bool HasHeaderRow { get; set; }

        public UserDiscoveryMode DiscoveryMode { get; set; }

        public List<Regex> PrincipalFilters { get; } = new List<Regex>();

        public List<Regex> ComputerFilters { get; } = new List<Regex>();

        public HashSet<SecurityIdentifier> PrincipalSidFilter { get; } = new HashSet<SecurityIdentifier>();

        public CancellationToken CancellationToken { get; set; } = CancellationToken.None;

        public string ImportOU { get; set; }
    }
}
