using System;
using System.Collections.Generic;
using Microsoft.Win32;

namespace Lithnet.AccessManager.Agent
{
    internal static class Extensions
    {
        public static T GetValue<T>(this RegistryKey key, string name)
        {
            return key.GetValue<T>(name, default(T));
        }

        public static T GetValue<T>(this RegistryKey key, string name, T defaultValue)
        {
            object rawvalue = key?.GetValue(name);

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

        public static IEnumerable<string> GetValues(this RegistryKey key, string name)
        {
            string[] rawvalue = key?.GetValue(name) as string[];

            if (rawvalue == null)
            {
                return new string[] { };
            }

            return rawvalue;
        }
    }
}
