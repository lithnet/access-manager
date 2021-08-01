using System.Collections.Generic;

namespace Lithnet.AccessManager.Server.Configuration
{
    public class AuthorizationServerMapping
    {
        public string Domain { get; set; }

        public bool DisableLocalFallback { get; set; }

        public bool DoNotRequireS4U { get; set; }

        public List<AuthorizationServer> Servers { get; set; } = new List<AuthorizationServer>();
    }
}
