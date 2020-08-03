using Microsoft.Win32;

namespace Lithnet.AccessManager.Agent
{
    public class LapsRegistrySettings : ILapsSettings
    {
        private const string policyKeyName = "SOFTWARE\\Policies\\Lithnet\\AccessManager\\Agent\\Password";
        private const string settingsKeyName = "SOFTWARE\\Lithnet\\AccessManager\\Agent\\Password";

        private readonly RegistryKey policyKey;

        private readonly RegistryKey settingsKey;

        public LapsRegistrySettings() :
            this(Registry.LocalMachine.OpenSubKey(policyKeyName, false), Registry.LocalMachine.CreateSubKey(settingsKeyName, true))
        {
        }

        public LapsRegistrySettings(RegistryKey policyKey, RegistryKey settingsKey)
        {
            this.policyKey = policyKey;
            this.settingsKey = settingsKey;
        }

        public PasswordStorageLocation StorageMode => (PasswordStorageLocation)(this.policyKey.GetValue<int>("StorageMode", 0));

        public string CertThumbprint => this.policyKey.GetValue<string>("CertThumbprint") ?? this.settingsKey.GetValue<string>("CertThumbprint");

        public string CertPath => this.policyKey.GetValue<string>("CertPath") ?? this.settingsKey.GetValue<string>("CertPath") ?? "encryption.cer";

        public bool CertDirectoryLookup => this.policyKey.GetValue<int>("CertDirectoryLookup", 1) == 1;

        public bool Enabled => this.policyKey.GetValue<int>("Enabled", 0) == 1;

        public int PasswordLength => this.policyKey.GetValue<int>("PasswordLength", 16);

        public string PasswordCharacters => this.policyKey.GetValue<string>("PasswordCharacters", null);

        public bool UseUpper => this.policyKey.GetValue<int>("UseUpper", 0) == 1;

        public bool UseLower => this.policyKey.GetValue<int>("UseLower", 0) == 1;

        public bool UseSymbol => this.policyKey.GetValue<int>("UseSymbol", 0) == 1;

        public bool UseNumeric => this.policyKey.GetValue<int>("UseNumeric", 0) == 1;

        public bool UseReadibilitySeparator => this.policyKey.GetValue<int>("UseReadibilitySeparator", 0) == 1;

        public string ReadabilitySeparator => this.policyKey.GetValue<string>("ReadabilitySeparator", "-");

        public int ReadabilitySeparatorInterval => this.policyKey.GetValue<int>("ReadabilitySeparatorInterval", 4);

        public int PasswordHistoryDaysToKeep => this.policyKey.GetValue<int>("PasswordHistoryDaysToKeep", 0);

        public bool WriteToMsMcsAdmPasswordAttributes => this.StorageMode.HasFlag(PasswordStorageLocation.MsLapsAttribute);

        public int MaximumPasswordAge => this.policyKey.GetValue<int>("MaximumPasswordAge", 14);

        public bool WriteToLithnetAttributes => this.StorageMode.HasFlag(PasswordStorageLocation.LithnetAttribute);
    }
}
