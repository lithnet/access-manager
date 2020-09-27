using System.Collections.Generic;
using Lithnet.AccessManager.Server.Configuration;

namespace Lithnet.AccessManager.Server.UI.AuthorizationRuleImport
{
    public class ImportResults
    {
        public List<SecurityDescriptorTarget> Targets { get; set; } = new List<SecurityDescriptorTarget>();

        public List<DiscoveryError> DiscoveryErrors { get; set; } = new List<DiscoveryError>();

        public NotificationChannels NotificationChannels { get; set; } = new NotificationChannels();
    }
}
