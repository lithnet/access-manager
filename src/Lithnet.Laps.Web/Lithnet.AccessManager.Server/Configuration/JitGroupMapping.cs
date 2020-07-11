using System.Security.Claims;
using Lithnet.AccessManager.Server;

namespace Lithnet.AccessManager.Configuration
{
    public class JitGroupMapping
    {
        public string ComputerOU { get; set; }

        public string GroupOU { get; set; }

        public string GroupNameTemplate { get; set; }

        public GroupType GroupType { get; set; }
    }
}