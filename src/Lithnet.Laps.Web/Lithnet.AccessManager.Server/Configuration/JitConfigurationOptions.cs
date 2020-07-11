using System.Collections.Generic;
using System.Security.Claims;

namespace Lithnet.AccessManager.Configuration
{
    public class JitConfigurationOptions
    {
        public IList<JitGroupMapping> JitGroupMappings { get; set; }

        public bool EnableJitGroupCreation { get; set; }

        public bool EnableJitGroupDeletion { get; set; }
    }
}