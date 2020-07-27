namespace Lithnet.AccessManager.Server.Configuration
{
    public class JitDynamicGroupMapping
    { 
        public string GroupOU { get; set; }

        public string GroupNameTemplate { get; set; }

        public string Description { get; set; }
        
        public string Domain { get; set; }

        public int? OverrideMode { get; set; }
    }
}