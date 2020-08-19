using System;
using System.Collections.Generic;
using Microsoft.Win32;

namespace Lithnet.AccessManager.Agent
{
    public abstract class RegistrySettings
    {
        private readonly string policyKeyName;

        protected RegistrySettings(string keyBaseName, bool relative)
        {
            if (relative)
            {
                this.policyKeyName = $"HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\{keyBaseName}";
            }
            else
            {
                this.policyKeyName = keyBaseName;
            }
        }

        protected T GetValue<T>(string valueName, T defaultValue)
        {
            object rawvalue = Registry.GetValue(policyKeyName, valueName, defaultValue);

            if (rawvalue == null)
            {
                return defaultValue;
            }

            try
            {
                return (T)Convert.ChangeType(rawvalue, typeof(T));
            }
            catch
            {
            }

            return defaultValue;
        }

        protected T GetValue<T>(string valueName)
        {
            object rawvalue = Registry.GetValue(policyKeyName, valueName, null);

            if (rawvalue == null)
            {
                return default;
            }

            try
            {
                return (T)Convert.ChangeType(rawvalue, typeof(T));
            }
            catch
            {
            }

            return default;
        }

        protected IEnumerable<string> GetValues(string name)
        {
            string[] rawvalue = Registry.GetValue(policyKeyName, name, null) as string[];

            if (rawvalue == null)
            {
                return new string[] { };
            }

            return rawvalue;
        }
    }
}
