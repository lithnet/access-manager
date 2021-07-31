using System;
using System.Collections.Generic;
using Microsoft.Win32;

namespace Lithnet.AccessManager.Agent.Providers
{
    public class RegistrySettingsProvider
    {
        private readonly string qualifiedKeyName;
        private readonly string keyName;
        private readonly RegistryHive hive;

        public RegistrySettingsProvider(string keyName)
        {
            this.qualifiedKeyName = keyName;

            if (keyName.StartsWith("HKEY_LOCAL_MACHINE", StringComparison.OrdinalIgnoreCase))
            {
                this.hive = RegistryHive.LocalMachine;
            }
            else if (keyName.StartsWith("HKEY_CURRENT_USER", StringComparison.OrdinalIgnoreCase))
            {
                this.hive = RegistryHive.CurrentUser;
            }
            else
            {
                throw new NotSupportedException("Invalid registry hive");
            }

            int index = this.qualifiedKeyName.IndexOf('\\');

            this.keyName = this.qualifiedKeyName.Remove(0, index + 1);
        }


        public void SetValue<T>(string valueName, T value)
        {
            if (value == null)
            {
                RegistryKey.OpenBaseKey(hive, RegistryView.Default).OpenSubKey(this.keyName, true)?.DeleteValue(valueName, false);
            }
            else
            {
                if (value is bool b)
                {
                    Registry.SetValue(this.qualifiedKeyName, valueName, b ? 1 : 0);
                }
                else
                {
                    Registry.SetValue(this.qualifiedKeyName, valueName, value);
                }
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
                Type ttype = typeof(T);

                if (ttype == typeof(bool))
                {
                    int val = (int)Convert.ChangeType(rawvalue, typeof(int));
                    return (T)(object)(val != 0);
                }
                else if (ttype.IsEnum)
                {
                    return (T)rawvalue;
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
