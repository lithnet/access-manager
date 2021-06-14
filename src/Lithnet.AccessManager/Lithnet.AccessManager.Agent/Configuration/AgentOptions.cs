using System;
using System.Collections.Generic;
using System.Text;

namespace Lithnet.AccessManager.Agent.Configuration
{
    public class AgentOptions
    {
        public int Interval { get; set; } = 60;

        public bool Enabled { get; set; } = true;

        public int CheckInIntervalHours { get; set; } = 24;

        public AdvancedAgentOptions AdvancedAgent { get; set; } = new AdvancedAgentOptions();

        public PasswordManagementOptions PasswordManagement { get; set; } = new PasswordManagementOptions();
    }
}
