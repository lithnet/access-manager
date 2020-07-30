using System.Collections.Generic;

namespace Lithnet.AccessManager.Server.Configuration
{
    public class BuiltInProviderOptions
    {
        public AclEvaluationLocation AccessControlEvaluationLocation { get; set; } 

        public IList<SecurityDescriptorTarget> Targets { get; set; }

        public int AuthZCacheDuration { get; set; }
    }
}
