using Lithnet.AccessManager.Api.Shared;
using Microsoft.Extensions.Logging;
using System;
using System.Net;

namespace Lithnet.AccessManager.Agent.Providers
{
    public class MacOsPlatformDataProvider : IPlatformDataProvider
    {
        private readonly ILogger<MacOsPlatformDataProvider> logger;
        private readonly ICommandLineRunner cmdlineRunner;

        private string osName = null;
        private string osVersion = null;

        public MacOsPlatformDataProvider(ILogger<MacOsPlatformDataProvider> logger, ICommandLineRunner cmdlineRunner)
        {
            this.logger = logger;
            this.cmdlineRunner = cmdlineRunner;
        }

        public string GetOSName()
        {
            try
            {
                if (this.osName != null)
                {
                    return this.osName;
                }

                this.osName = this.ExecuteCommandLine("sw_vers", "-productName");
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(ex, "Could not obtain OS product name");
            }

            this.osName ??= "macOS";

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

                this.osVersion = this.ExecuteCommandLine("sw_vers", "-productVersion");
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
            return OsType.MacOS;
        }

        private string ExecuteCommandLine(string cmd, string arg)
        {
            var result = this.cmdlineRunner.ExecuteCommand(cmd, arg);

            result.EnsureSuccess();

            return result.StdOut;
        }
    }
}
