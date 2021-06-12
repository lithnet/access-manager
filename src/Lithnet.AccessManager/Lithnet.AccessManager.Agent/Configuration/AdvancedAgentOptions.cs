using System;
using System.Collections.Generic;
using System.Text;

namespace Lithnet.AccessManager.Agent.Configuration
{
    public class AdvancedAgentOptions 
    {
        public string Server { get; set; }

        public bool Enabled { get; set; }

        public AuthenticationMode AuthenticationMode { get; set; }

        public IList<string> AadIssuerDNs { get; set; } = new List<string> { "DC=net,DC=windows,CN=MS-Organization-Access,OU=82dbaca4-3e81-46ca-9c73-0950c1eaca97" };
    }
}
