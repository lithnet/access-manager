using System.Collections.Generic;

namespace Lithnet.AccessManager.Server.Configuration
{
    public class JitConfigurationOptions
    {
        public IList<JitGroupMapping> JitGroupMappings { get; set; } = new List<JitGroupMapping>();

        public bool EnableJitGroupCreation { get; set; }

        public bool EnableJitGroupDeletion { get; set; }
    }
}