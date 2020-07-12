using Lithnet.AccessManager.Server.Configuration;

namespace Lithnet.AccessManager.Server.Configuration
{
    public class JitGroupMapping
    {
        public string ComputerOU { get; set; }

        public string GroupOU { get; set; }

        public string GroupNameTemplate { get; set; }

        public GroupType GroupType { get; set; }
    }
}