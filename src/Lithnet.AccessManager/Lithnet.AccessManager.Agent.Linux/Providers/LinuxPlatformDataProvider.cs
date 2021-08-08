using Lithnet.AccessManager.Api.Shared;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Text.RegularExpressions;

namespace Lithnet.AccessManager.Agent.Providers
{
    public class LinuxPlatformDataProvider : IPlatformDataProvider
    {
        private readonly ILogger<LinuxPlatformDataProvider> logger;

        private string osName = null;
        private string osVersion = null;
        private string osData = null;

        public LinuxPlatformDataProvider(ILogger<LinuxPlatformDataProvider> logger)
        {
            this.logger = logger;
        }

        public string GetOSName()
        {
            try
            {
                if (this.osName != null)
                {
                    return this.osName;
                }

                this.osName = this.GetValueFromOsData("NAME");
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(ex, "Could not obtain OS distribution name");
            }

            this.osName ??= "Linux";

            return this.osName;
        }

        public string GetOSVersion()
        {
            try
            {
                if (this.osVersion != null)
                {
                    return this.osVersion;
                }

                this.osVersion = this.GetValueFromOsData("VERSION_ID");
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(ex, "Could not obtain OS version. Defaulting to kernel version");
            }

            this.osVersion ??= Environment.OSVersion.Version.ToString();

            return this.osVersion;
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
                return this.GetMachineName();
            }

            return dnsName;
        }

        public OsType GetOsType()
        {
            return OsType.Linux;
        }

        private void PopulateOsData()
        {
            if (this.osData != null)
            {
                return;
            }

            try
            {
                if (System.IO.File.Exists("/etc/os-release"))
                {
                    this.osData = System.IO.File.ReadAllText("/etc/os-release");
                }
                else
                {
                    this.osData = System.IO.File.ReadAllText("/usr/lib/os-release");
                }
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(ex, "Could not obtain OS data");
            }
        }

        private string GetValueFromOsData(string valueName)
        {
            if (string.IsNullOrWhiteSpace(this.osData))
            {
                this.PopulateOsData();
                if (string.IsNullOrWhiteSpace(this.osData))
                {
                    this.logger.LogTrace("No OS data available to parse");
                    return null;
                }
            }

            var match = Regex.Match(this.osData, $"^{valueName}=\"?(?<value>.+?)\"?$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.ExplicitCapture);
            if (match.Success)
            {
                return match.Groups["value"].Value;
            }

            this.logger.LogWarning($"Could not find request key '{valueName}' in the OS data set");

            return null;
        }
    }
}
