using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Lithnet.AccessManager.Server;

namespace Lithnet.AccessManager.Configuration
{
    public class BuiltInProviderOptions
    {
        public AclEvaluationLocation AccessControlEvaluationLocation { get; set; } 

        public IList<SecurityDescriptorTarget> Targets { get; set; }
    }
}
