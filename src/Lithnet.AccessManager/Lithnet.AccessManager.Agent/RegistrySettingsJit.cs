using System.Collections.Generic;
using Microsoft.Win32;

namespace Lithnet.AccessManager.Agent
{
    public class RegistrySettingsJit : RegistrySettings, IJitSettings
    {
        private const string keyName = "Lithnet\\Access Manager Agent\\Jit";

        public RegistrySettingsJit() : base(keyName, true)
        {
        }

        internal RegistrySettingsJit(string key) : base(key, false)
        {
        }

        public bool RestrictAdmins => this.GetValue<int>("RestrictAdmins", 0) == 1;

        public bool JitEnabled => this.GetValue<int>("Enabled", 0) == 1;

        public string JitGroup => this.GetValue<string>("JitGroup");

        public IEnumerable<string> AllowedAdmins => this.GetValues("AllowedAdmins");
    }
}
