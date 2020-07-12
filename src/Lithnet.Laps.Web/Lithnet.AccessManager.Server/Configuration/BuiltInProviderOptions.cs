using System.Collections.Generic;
using Lithnet.AccessManager.Server.Configuration;

namespace Lithnet.AccessManager.Server.Configuration
{
    public class BuiltInProviderOptions
    {
        public AclEvaluationLocation AccessControlEvaluationLocation { get; set; } 

        public IList<SecurityDescriptorTarget> Targets { get; set; }
    }
}
