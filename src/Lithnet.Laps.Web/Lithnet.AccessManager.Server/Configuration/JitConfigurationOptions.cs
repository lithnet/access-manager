using System.Collections.Generic;

namespace Lithnet.AccessManager.Server.Configuration
{
    public class JitConfigurationOptions
    {
        public IList<JitGroupMapping> JitGroupMappings { get; set; } = new List<JitGroupMapping>();

        public bool EnableJitGroupCreation { get; set; }

        public int? FullSyncInterval { get; set; }

        public int? DeltaSyncInterval { get; set; }
    }
}