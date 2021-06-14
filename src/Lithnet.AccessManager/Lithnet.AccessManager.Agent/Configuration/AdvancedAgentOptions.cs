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
    }
}
