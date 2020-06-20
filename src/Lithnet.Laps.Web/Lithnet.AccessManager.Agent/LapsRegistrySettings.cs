using System.Collections.Generic;
using Microsoft.Win32;

namespace Lithnet.AccessManager.Agent
{
    internal class LapsRegistrySettings : ILapsSettings
    {
        private const string policyKeyName = "SOFTWARE\\Policies\\Lithnet\\AccessManager\\Agent\\Laps";
        private const string settingsKeyName = "SOFTWARE\\Lithnet\\AccessManager\\Agent\\Laps";

        private RegistryKey policyKey;

        private RegistryKey settingsKey;

        public LapsRegistrySettings() :
            this(Registry.LocalMachine.OpenSubKey(policyKeyName, false), Registry.LocalMachine.CreateSubKey(settingsKeyName, true))
        { 
        }

        public LapsRegistrySettings(RegistryKey policyKey, RegistryKey settingsKey)
        {
            this.policyKey = policyKey;
            this.settingsKey = settingsKey;
        }

        public string SigningCertThumbprint => this.policyKey.GetValue<string>("SigningCertThumbprint");

        public bool LapsEnabled => this.policyKey.GetValue<int>("LapsEnabled", 0) == 1;

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

        public bool WriteToMsMcsAdmPasswordAttributes => this.policyKey.GetValue<int>("WriteToMsMcsAdmPasswordAttributes", 0) == 1;

        public int MaximumPasswordAge => this.policyKey.GetValue<int>("MaximumPasswordAge", 14);

        public bool WriteToAppData => this.policyKey.GetValue<int>("WriteToAppData", 0) == 1;
    }
}
