using System.Collections.Generic;
using Lithnet.AccessManager.Server.Configuration;

namespace Lithnet.AccessManager.Server.UI.AuthorizationRuleImport
{
    public class ImportResults
    {
        public List<SecurityDescriptorTarget> Targets { get; set; }

        public List<DiscoveryError> DiscoveryErrors { get; set; }
    }
}
