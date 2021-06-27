using Microsoft.Win32;
using System;
using System.IO;

namespace Lithnet.AccessManager.Server
{
    public class RegistryProvider : IRegistryProvider
    {
        public const string BaseKey = "Software\\Lithnet\\Access Manager Service";

        public const string ParametersKey = "Software\\Lithnet\\Access Manager Service\\Parameters";

        protected readonly RegistryKey baseKey;
        protected readonly RegistryKey paramsKey;

        public RegistryProvider(bool writable)
        {
            if (writable)
            {
                paramsKey = Registry.LocalMachine.CreateSubKey(ParametersKey);
                baseKey = Registry.LocalMachine.CreateSubKey(BaseKey);
            }
            else
            {
                paramsKey = Registry.LocalMachine.OpenSubKey(ParametersKey, false);
                baseKey = Registry.LocalMachine.OpenSubKey(BaseKey, false);
            }
        }

        public string BasePath => baseKey?.GetValue("BasePath") as string;

        public string ConfigPath => paramsKey?.GetValue("ConfigPath") as string;

        public string LogPath => paramsKey?.GetValue("LogPath") as string ?? Path.Combine(Directory.GetCurrentDirectory(), "logs");

        public int RetentionDays => Math.Max(paramsKey?.GetValue("LogRetentionDays") as int? ?? 7, 1);

        public bool IsConfigured
        {
            get => (baseKey?.GetValue("Configured", 0) is int value) && value == 1;
            set => baseKey.SetValue("Configured", value ? 1 : 0);
        }

        public bool DeleteLocalDbInstance
        {
            get => (baseKey?.GetValue("DeleteLocalDbInstance", 0) is int value) && value == 1;
            set
            {
                if (!value)
                {
                    baseKey.DeleteValue("DeleteLocalDbInstance", false);
                }
                else
                {
                    baseKey.SetValue("DeleteLocalDbInstance", 1);
                }
            }
        }

        public bool ResetScheduler
        {
            get => (paramsKey?.GetValue("ResetScheduler", 0) is int value) && value == 1;
            set
            {
                if (!value)
                {
                    paramsKey.DeleteValue("ResetScheduler", false);
                }
                else
                {
                    paramsKey.SetValue("ResetScheduler", 1);
                }
            }
        }

        public bool ApiEnabled
        {
            get => (paramsKey?.GetValue("ApiEnabled", 0) is int value) && value == 1;
            set
            {
                if (!value)
                {
                    paramsKey.DeleteValue("ApiEnabled", false);
                }
                else
                {
                    paramsKey.SetValue("ApiEnabled", 1);
                }
            }
        }

        public bool ResetMaintenanceTaskSchedules
        {
            get => (paramsKey?.GetValue("ResetMaintenanceTaskSchedules", 0) is int value) && value == 1;
            set
            {
                if (!value)
                {
                    paramsKey.DeleteValue("ResetMaintenanceTaskSchedules", false);
                }
                else
                {
                    paramsKey.SetValue("ResetMaintenanceTaskSchedules", 1);
                }
            }
        }

        public int CacheMode
        {
            get => (int?)paramsKey?.GetValue("RateLimitCacheMode", 0) ?? 0;
            set => paramsKey.SetValue("RateLimitCacheMode", value);
        }

        public string HttpAcl
        {
            get => baseKey?.GetValue("HttpAcl") as string;
            set
            {
                if (value == null)
                {
                    baseKey.DeleteValue("HttpAcl", false);
                }
                else
                {
                    baseKey.SetValue("HttpAcl", value);
                }
            }
        }

        public string LastNotifiedVersion
        {
            get => paramsKey?.GetValue("LastNotifiedVersion") as string;
            set
            {
                if (value == null)
                {
                    paramsKey.DeleteValue("LastNotifiedVersion", false);
                }
                else
                {
                    paramsKey.SetValue("LastNotifiedVersion", value);
                }
            }
        }

        public string LastNotifiedCertificateKey
        {
            get => paramsKey?.GetValue("LastNotifiedCertificateKey") as string;
            set
            {
                if (value == null)
                {
                    paramsKey.DeleteValue("LastNotifiedCertificateKey", false);
                }
                else
                {
                    paramsKey.SetValue("LastNotifiedCertificateKey", value);
                }
            }
        }

        public string ServiceKeyThumbprint
        {
            get => paramsKey?.GetValue("ServiceKeyThumbprint") as string;
            set
            {
                if (value == null)
                {
                    paramsKey.DeleteValue("ServiceKeyThumbprint", false);
                }
                else
                {
                    paramsKey.SetValue("ServiceKeyThumbprint", value);
                }
            }
        }

        public string HttpsAcl
        {
            get => baseKey?.GetValue("HttpsAcl") as string;
            set
            {
                if (value == null)
                {
                    baseKey.DeleteValue("HttpsAcl", false);
                }
                else
                {
                    baseKey.SetValue("HttpsAcl", value);
                }
            }
        }

        public string CertBinding
        {
            get => baseKey?.GetValue("CertBinding") as string;
            set
            {
                if (value == null)
                {
                    baseKey.DeleteValue("CertBinding", false);
                }
                else
                {
                    baseKey.SetValue("CertBinding", value);
                }
            }
        }
    }
}
