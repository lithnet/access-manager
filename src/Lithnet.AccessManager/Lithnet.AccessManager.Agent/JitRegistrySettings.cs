using System.Collections.Generic;
using Microsoft.Win32;

namespace Lithnet.AccessManager.Agent
{
    public class JitRegistrySettings : IJitSettings
    {
        private const string policyKeyName = "SOFTWARE\\Policies\\Lithnet\\Access Manager Agent\\Jit";
        private const string settingsKeyName = "SOFTWARE\\Lithnet\\Access Manager Agent\\Jit";

        private readonly RegistryKey policyKey;

        private readonly RegistryKey settingsKey;

        public JitRegistrySettings()
        {
            var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            policyKey = baseKey.OpenSubKey(policyKeyName);
            settingsKey = baseKey.OpenSubKey(settingsKeyName);
        }

        public JitRegistrySettings(RegistryKey policyKey, RegistryKey settingsKey)
        {
            this.policyKey = policyKey;
            this.settingsKey = settingsKey;
        }

        public bool RestrictAdmins => this.policyKey.GetValue<int>("RestrictAdmins", 0) == 1;

        public bool JitEnabled => this.policyKey.GetValue<int>("Enabled", 0) == 1;

        public string JitGroup => this.policyKey.GetValue<string>("JitGroup");

        public IEnumerable<string> AllowedAdmins => this.policyKey.GetValues("AllowedAdmins");
    }
}
