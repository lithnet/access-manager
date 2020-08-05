using Microsoft.Win32;

namespace Lithnet.AccessManager.Agent
{
    public class AgentRegistrySettings : IAgentSettings
    {
        private const string policyKeyName = "SOFTWARE\\Policies\\Lithnet\\Access Manager Agent";
        private const string settingsKeyName = "SOFTWARE\\Lithnet\\Access Manager Agent";

        private readonly RegistryKey policyKey;

        private readonly RegistryKey settingsKey;


        public AgentRegistrySettings()
        {
            var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            policyKey = baseKey.OpenSubKey(policyKeyName);
            settingsKey = baseKey.OpenSubKey(settingsKeyName);
        }

        public AgentRegistrySettings(RegistryKey policyKey, RegistryKey settingsKey)
        {
            this.policyKey = policyKey;
            this.settingsKey = settingsKey;
        }
    
        public bool Enabled => this.policyKey.GetValue<int>("Enabled", 0) == 1;

        public int Interval => this.policyKey.GetValue<int>("Interval", 1);
    }
}
