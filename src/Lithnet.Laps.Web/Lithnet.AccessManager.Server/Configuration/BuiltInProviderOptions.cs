using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Lithnet.AccessManager.Configuration
{
    public class BuiltInProviderOptions
    {
        public IList<SecurityDescriptorTarget> Targets { get; set; }
    }
}
