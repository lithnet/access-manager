using System;
using System.Collections.Generic;
using System.Text;
using Lithnet.AccessManager.Api.Shared;

namespace Lithnet.AccessManager.Agent.Configuration
{
    public class AdvancedAgentOptions 
    {
        public string Server { get; set; }

        public bool Enabled { get; set; }

        public AgentAuthenticationMode AuthenticationMode { get; set; }
    }
}
