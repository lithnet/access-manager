using System.Collections.Generic;
using Microsoft.Win32;

namespace Lithnet.AccessManager.Agent
{
    public class JitRegistrySettings : IJitSettings
    {
        private const string policyKeyName = "SOFTWARE\\Policies\\Lithnet\\AccessManager\\Agent\\Jit";
        private const string settingsKeyName = "SOFTWARE\\Lithnet\\AccessManager\\Agent\\Jit";

        private readonly RegistryKey policyKey;

        private readonly RegistryKey settingsKey;

        public JitRegistrySettings() :
            this(Registry.LocalMachine.OpenSubKey(policyKeyName, false),
                Registry.LocalMachine.CreateSubKey(settingsKeyName, true))
        {
        }

        public JitRegistrySettings(RegistryKey policyKey, RegistryKey settingsKey)
        {
            this.policyKey = policyKey;
            this.settingsKey = settingsKey;
        }

        public bool RestrictAdmins => this.policyKey.GetValue<int>("RestrictAdmins", 0) == 1;

        public bool JitEnabled => this.policyKey.GetValue<int>("JitEnabled", 0) == 1;

        public string JitGroup => this.policyKey.GetValue<string>("JitGroup");

        public IEnumerable<string> AllowedAdmins => this.policyKey.GetValues("AllowedAdmins");
    }
}
