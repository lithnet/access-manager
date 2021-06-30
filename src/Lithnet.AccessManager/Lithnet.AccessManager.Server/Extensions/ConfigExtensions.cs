using System;
using System.Collections.Generic;
using System.Linq;
using Lithnet.AccessManager.Server.Configuration;
using Microsoft.Extensions.Configuration;

namespace Lithnet.AccessManager.Server
{
    public static class ConfigExtensions
    {
        public static IEnumerable<string> GetValuesOrDefault(this IConfiguration config, string key, params string[] defaultValues)
        {
            string value = config[key];

            if (value == null)
            {
                var values = config.GetSection(key)?.GetChildren()?.ToList();
                if (values != null && values.Count > 0)
                {
                    foreach (var item in values)
                    {
                        yield return item.Value;
                    }

                    yield break;
                }
            }

            foreach (string dv in defaultValues ?? new string[] { })
            {
                yield return dv;
            }
        }

        public static TEnum GetValueOrDefault<TEnum>(this IConfiguration config, string key, TEnum defaultValue) where TEnum : struct, Enum
        {
            string value = config[key];

            if (value == null)
            {
                var values = config.GetSection(key)?.GetChildren();
                value = string.Join(',', values.Select(t => t.Value));
            }

            if (Enum.TryParse(typeof(TEnum), value, true, out object result))
            {
                if (result == null)
                {
                    return defaultValue;
                }

                return (TEnum)result;
            }

            return defaultValue;
        }

        public static bool GetValueOrDefault(this IConfiguration config, string key, bool defaultValue)
        {
            string value = config[key];

            if (bool.TryParse(value, out bool result))
            {
                return result;
            }

            return defaultValue;
        }

        public static int GetValueOrDefault(this IConfiguration config, string key, int defaultValue)
        {
            string value = config[key];

            if (int.TryParse(value, out int result))
            {
                return result;
            }

            return defaultValue;
        }

        public static int GetValueOrDefault(this IConfiguration config, string key, int minimumValue, int defaultValue)
        {
            string value = config[key];

            if (int.TryParse(value, out int result))
            {
                if (result < minimumValue)
                {
                    return defaultValue;
                }

                return result;
            }

            return defaultValue;
        }
        public static bool IsAdTarget(this TargetType t)
        {
            return t == TargetType.AdComputer || t == TargetType.AdContainer || t == TargetType.AdGroup;
        }

        public static bool IsAadTarget(this TargetType t)
        {
            return t == TargetType.AadComputer || t == TargetType.AadGroup;
        }

        public static bool IsAmsTarget(this TargetType t)
        {
            return t == TargetType.AmsComputer || t == TargetType.AmsGroup;
        }
    }
}