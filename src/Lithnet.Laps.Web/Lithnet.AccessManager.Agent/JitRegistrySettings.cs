using System.Collections.Generic;
using Microsoft.Win32;

namespace Lithnet.AccessManager.Agent
{
    internal class JitRegistrySettings : IJitSettings
    {
        private const string policyKeyName = "SOFTWARE\\Policies\\Lithnet\\AccessManager\\Agent\\Jit";
        private const string settingsKeyName = "SOFTWARE\\Lithnet\\AccessManager\\Agent\\Jit";

        private readonly RegistryKey policyKey;

        private readonly RegistryKey settingsKey;

        public JitRegistrySettings() :
           this(Registry.LocalMachine.OpenSubKey(policyKeyName, false), Registry.LocalMachine.CreateSubKey(settingsKeyName, true))
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

        public bool CreateJitGroup => this.policyKey.GetValue<int>("CreateJitGroup", 0) == 1;
        
        public string JitGroupCreationOU => this.policyKey.GetValue<string>("JitGroupCreationOU");

        public IEnumerable<string> AllowedAdmins => this.policyKey.GetValues("AllowedAdmins");

        public int JitGroupType => this.policyKey.GetValue<int>("JitGroupType", -2147483644);

        public string JitGroupDescription => this.policyKey.GetValue<string>("JitGroupDescription", "JIT access group created by Lithnet Access Manager");
    }
}
