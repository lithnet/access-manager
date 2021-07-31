using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Lithnet.AccessManager.Agent.Providers
{
    public class WindowsPlatformDataProvider : IPlatformDataProvider
    {
        public string GetOSName()
        {
            return "Windows";
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
    }
}
