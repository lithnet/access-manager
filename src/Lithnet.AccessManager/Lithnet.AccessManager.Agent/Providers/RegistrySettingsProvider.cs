using System;
using System.Collections.Generic;
using Microsoft.Win32;

namespace Lithnet.AccessManager.Agent
{
    public class RegistrySettingsProvider
    {
        private readonly string policyKeyName;

        public RegistrySettingsProvider(string keyBaseName, bool relative)
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

        public T GetValue<T>(string valueName, T defaultValue)
        {
            object rawvalue = Registry.GetValue(this.policyKeyName, valueName, defaultValue);

            if (rawvalue == null)
            {
                return defaultValue;
            }

            try
            {
                if (typeof(T) == typeof(bool))
                {
                    int val = (int)Convert.ChangeType(rawvalue, typeof(int));
                    return (T)(object)(val != 0);
                }
                else
                {
                    return (T)Convert.ChangeType(rawvalue, typeof(T));
                }
            }
            catch
            {
            }

            return defaultValue;
        }

        public T GetValue<T>(string valueName)
        {
            object rawvalue = Registry.GetValue(this.policyKeyName, valueName, null);

            if (rawvalue == null)
            {
                return default;
            }

            try
            {
                if (typeof(T) == typeof(bool))
                {
                    int val = (int)Convert.ChangeType(rawvalue, typeof(int));
                    return (T)(object)(val != 0);
                }
                else
                {
                    return (T)Convert.ChangeType(rawvalue, typeof(T));
                }
            }
            catch
            {
            }

            return default;
        }

        public IEnumerable<string> GetValues(string name)
        {
            string[] rawvalue = Registry.GetValue(this.policyKeyName, name, null) as string[];

            if (rawvalue == null)
            {
                return new string[] { };
            }

            return rawvalue;
        }
    }
}
