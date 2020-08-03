using Microsoft.Win32;

namespace Lithnet.AccessManager.Agent
{
    public class AgentRegistrySettings : IAgentSettings
    {
        private const string policyKeyName = "SOFTWARE\\Policies\\Lithnet\\AccessManager\\Agent";
        private const string settingsKeyName = "SOFTWARE\\Lithnet\\AccessManager\\Agent";

        private readonly RegistryKey policyKey;

        private readonly RegistryKey settingsKey;


        public AgentRegistrySettings() :
           this(Registry.LocalMachine.OpenSubKey(policyKeyName, false), Registry.LocalMachine.CreateSubKey(settingsKeyName, true))
        {
        }

        public AgentRegistrySettings(RegistryKey policyKey, RegistryKey settingsKey)
        {
            this.policyKey = policyKey;
            this.settingsKey = settingsKey;
        }
    
        public bool Enabled => this.policyKey.GetValue<int>("Enabled", 0) == 1;

        public int Interval => this.policyKey.GetValue<int>("Interval", 60);
    }
}
