using System.Collections.Generic;
using Microsoft.Win32;

namespace Lithnet.AccessManager.Agent
{
    internal class RegistrySettingsProvider : ISettingsProvider
    {
        private const string policyKeyName = "SOFTWARE\\Policies\\Lithnet\\AccessManager\\Agent";
        private const string settingsKeyName = "SOFTWARE\\Lithnet\\AccessManager\\Agent";

        private RegistryKey policyKey;

        private RegistryKey settingsKey;

        public RegistrySettingsProvider()
        {
            this.policyKey = Registry.LocalMachine.OpenSubKey(policyKeyName, false);
            this.settingsKey = Registry.LocalMachine.CreateSubKey(settingsKeyName, true);
        }

        public bool Enabled => this.policyKey.GetValue<int>("Enabled", 0) == 1;

        public bool RemoveUnmanagedMembers => this.policyKey.GetValue<int>("RemoveUnmanagedMembers", 0) == 1;

        public string JitGroup => this.policyKey.GetValue<string>("JitGroup");

        public bool CreateGroup => this.policyKey.GetValue<int>("CreateGroup", 0) == 1;
        
        public bool PublishLamObject => this.policyKey.GetValue<int>("PublishLamObject", 1) == 1;

        public string GroupNameTemplate => this.policyKey.GetValue<string>("CreateGroupNameTemplate");

        public string GroupCreateOu => this.policyKey.GetValue<string>("CreateGroupOU");

        public IEnumerable<string> AllowedAdmins => this.policyKey.GetValues("AllowedAdmins");

        public int GroupType => this.policyKey.GetValue<int>("CreateGroupType", -2147483644);

        public int CheckInterval => this.policyKey.GetValue<int>("CheckInterval", 60);

        public string CachedGroupSid { get => this.settingsKey.GetValue<string>("GroupSid"); set => this.settingsKey.SetValue("GroupSid", value, RegistryValueKind.String); }

        public string CachedGroupName { get => this.settingsKey.GetValue<string>("GroupName"); set => this.settingsKey.SetValue("GroupName", value, RegistryValueKind.String); }
    }
}
