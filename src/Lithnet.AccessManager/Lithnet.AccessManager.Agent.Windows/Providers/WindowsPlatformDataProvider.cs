using Lithnet.AccessManager.Api.Shared;
using System;
using System.Net;
using Microsoft.Win32;

namespace Lithnet.AccessManager.Agent.Providers
{
    public class WindowsPlatformDataProvider : IPlatformDataProvider
    {
        public string GetOSName()
        {
            return Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ProductName", null) as string ?? "Windows";
        }

        public string GetOSVersion()
        {
            return Environment.OSVersion.Version.ToString();
        }

        public string GetMachineName()
        {
            return Environment.MachineName;
        }

        public string GetDnsName()
        {
            string dnsName = Dns.GetHostEntry("LocalHost").HostName;
            if (string.Equals(dnsName, "localhost", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return dnsName;
        }

        public OsType GetOsType()
        {
            return OsType.Windows;
        }
    }
}
