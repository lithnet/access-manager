using Microsoft.Win32;

namespace Lithnet.AccessManager.Agent
{
    public class LapsRegistrySettings : ILapsSettings
    {
        private const string policyKeyName = "SOFTWARE\\Policies\\Lithnet\\Access Manager Agent\\Password";
        private const string settingsKeyName = "SOFTWARE\\Lithnet\\Access Manager Agent\\Password";

        private readonly RegistryKey policyKey;

        private readonly RegistryKey settingsKey;

        public LapsRegistrySettings() 
        {
            var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            policyKey = baseKey.OpenSubKey(policyKeyName);
            settingsKey = baseKey.OpenSubKey(settingsKeyName);
        }

        public LapsRegistrySettings(RegistryKey policyKey, RegistryKey settingsKey)
        {
            this.policyKey = policyKey;
            this.settingsKey = settingsKey;
        }

        public bool Enabled => this.policyKey.GetValue<int>("Enabled", 0) == 1;

        public int PasswordLength => this.policyKey.GetValue<int>("PasswordLength", 16);

        public string PasswordCharacters => this.policyKey.GetValue<string>("PasswordCharacters", null);

        public bool UseUpper => this.policyKey.GetValue<int>("UseUpper", 0) == 1;

        public bool UseLower => this.policyKey.GetValue<int>("UseLower", 0) == 1;

        public bool UseSymbol => this.policyKey.GetValue<int>("UseSymbol", 0) == 1;

        public bool UseNumeric => this.policyKey.GetValue<int>("UseNumeric", 0) == 1;

        public bool UseReadabilitySeparator => this.policyKey.GetValue<int>("UseReadabilitySeparator", 0) == 1;

        public string ReadabilitySeparator => this.policyKey.GetValue<string>("ReadabilitySeparator", "-");

        public int ReadabilitySeparatorInterval => this.policyKey.GetValue<int>("ReadabilitySeparatorInterval", 4);

        public int PasswordHistoryDaysToKeep => this.policyKey.GetValue<int>("PasswordHistoryDaysToKeep", 0);
        
        public int MaximumPasswordAge => this.policyKey.GetValue<int>("MaximumPasswordAge", 14);

        public MsMcsAdmPwdBehaviour MsMcsAdmPwdBehaviour => (MsMcsAdmPwdBehaviour)(this.policyKey.GetValue<int>("MsMcsAdmPwdBehaviour", 0));
    }
}
