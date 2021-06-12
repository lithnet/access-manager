using System;
using System.Collections.Generic;
using System.Text;

namespace Lithnet.AccessManager.Agent.Configuration
{
    public class AgentOptions
    {
        public int Interval { get; set; }

        public bool Enabled { get; set; }

        public AdvancedAgentOptions AdvancedAgent { get; set; } = new AdvancedAgentOptions();

        public PasswordManagementOptions PasswordManagement { get; set; } = new PasswordManagementOptions();
    }
}
