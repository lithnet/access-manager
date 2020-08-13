using System.Collections.Generic;
using Microsoft.Win32;

namespace Lithnet.AccessManager.Agent
{
    public class RegistrySettingsJit : RegistrySettings, IJitSettings
    {
        private const string keyName = "Lithnet\\Access Manager Agent\\Jit";

        public RegistrySettingsJit() : base(keyName)
        {
        }

        public bool RestrictAdmins => this.GetKey().GetValue<int>("RestrictAdmins", 0) == 1;

        public bool JitEnabled => this.GetKey().GetValue<int>("Enabled", 0) == 1;

        public string JitGroup => this.GetKey().GetValue<string>("JitGroup");

        public IEnumerable<string> AllowedAdmins => this.GetKey().GetValues("AllowedAdmins");
    }
}
