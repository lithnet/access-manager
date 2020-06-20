using System.Collections.Generic;
using Microsoft.Win32;

namespace Lithnet.AccessManager.Agent
{
    internal class AgentRegistrySettings : IAgentSettings
    {
        private const string policyKeyName = "SOFTWARE\\Policies\\Lithnet\\AccessManager\\Agent";
        private const string settingsKeyName = "SOFTWARE\\Lithnet\\AccessManager\\Agent";

        private RegistryKey policyKey;

        private RegistryKey settingsKey;

        public AgentRegistrySettings()
        {
            this.policyKey = Registry.LocalMachine.OpenSubKey(policyKeyName, false);
            this.settingsKey = Registry.LocalMachine.CreateSubKey(settingsKeyName, true);
        }

        public bool Enabled => this.policyKey.GetValue<int>("Enabled", 0) == 1;

        public int CheckInterval => this.policyKey.GetValue<int>("CheckInterval", 60);
    }
}
