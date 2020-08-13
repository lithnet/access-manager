using Microsoft.Win32;

namespace Lithnet.AccessManager.Agent
{
    public abstract class RegistrySettings
    {
        private readonly string policyKeyName;
        
        protected RegistrySettings(string keyBaseName)
        {
            this.policyKeyName = $"SOFTWARE\\Policies\\{keyBaseName}";
        }
    
        protected RegistryKey GetKey()
        {
            var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            return baseKey.OpenSubKey(policyKeyName);
        }
    }
}
