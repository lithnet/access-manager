using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Win32;

namespace Lithnet.AccessManager.Server
{
    public class RegistryProvider : IRegistryProvider
    {
        public const string BaseKey = "Software\\Lithnet\\Access Manager Service";

        public const string ParametersKey = "Software\\Lithnet\\Access Manager Service\\Parameters";

        private readonly RegistryKey baseKey;
        private readonly RegistryKey paramsKey;

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

        public string HttpAcl
        {
            get => baseKey?.GetValue("HttpAcl") as string;
            set
            {
                if (value == null)
                {
                    baseKey.DeleteValue("HttpAcl");
                }
                else
                {
                    baseKey.SetValue("HttpAcl", value);
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
                    paramsKey.DeleteValue("ServiceKeyThumbprint");
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
                    baseKey.DeleteValue("HttpsAcl");
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
                    baseKey.DeleteValue("CertBinding");
                }
                else
                {
                    baseKey.SetValue("CertBinding", value);
                }
            }
        }
    }
}
