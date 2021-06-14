using System;
using System.Collections.Generic;
using Microsoft.Win32;

namespace Lithnet.AccessManager.Agent
{
    public class RegistrySettingsProvider
    {
        private readonly string qualifiedKeyName;
        private readonly string keyName;

        public RegistrySettingsProvider(string keyBaseName)
        {
            this.qualifiedKeyName = $"HKEY_LOCAL_MACHINE\\SOFTWARE\\{keyBaseName}";
            this.keyName = $"SOFTWARE\\{keyBaseName}";
        }

        public void SetValue<T>(string valueName, T value)
        {
            if (value == null)
            {
                RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default).OpenSubKey(this.keyName, true)?.DeleteValue(valueName, false);
            }
            else
            {
                Registry.SetValue(this.qualifiedKeyName, valueName, value);
            }
        }

        public T GetValue<T>(string valueName, T defaultValue)
        {
            object rawvalue = Registry.GetValue(this.qualifiedKeyName, valueName, defaultValue);

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
            object rawvalue = Registry.GetValue(this.qualifiedKeyName, valueName, null);

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
            string[] rawvalue = Registry.GetValue(this.qualifiedKeyName, name, null) as string[];

            if (rawvalue == null)
            {
                return new string[] { };
            }

            return rawvalue;
        }
    }
}
