using Microsoft.Win32;

namespace Lithnet.AccessManager.Agent
{
    public class RegistrySettingsAgent : RegistrySettings, IAgentSettings
    {
        private const string keyName = "Lithnet\\Access Manager Agent";

        public RegistrySettingsAgent() : base(keyName)
        {
        }

        public bool Enabled => this.GetKey().GetValue<int>("Enabled", 0) == 1;

        public int Interval => this.GetKey().GetValue<int>("Interval", 60);
    }
}
