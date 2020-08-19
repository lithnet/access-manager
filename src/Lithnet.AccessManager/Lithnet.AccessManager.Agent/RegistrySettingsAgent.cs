using Microsoft.Win32;

namespace Lithnet.AccessManager.Agent
{
    public class RegistrySettingsAgent : RegistrySettings, IAgentSettings
    {
        private const string keyName = "Lithnet\\Access Manager Agent";

        public RegistrySettingsAgent() : base(keyName, true)
        {
        }

        internal RegistrySettingsAgent(string key) : base(key, false)
        {
        }

        public bool Enabled => this.GetValue<int>("Enabled", 0) == 1;

        public int Interval => this.GetValue<int>("Interval", 60);
    }
}
