using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Lithnet.AccessManager.Agent.Configuration;
using Microsoft.Extensions.Options;

namespace Lithnet.AccessManager.Agent.Providers
{
    public class MacOsPasswordChangeProvider : IPasswordChangeProvider
    {
        private readonly ILogger<MacOsPasswordChangeProvider> logger;
        private readonly int timeout;
        private readonly UnixOptions unixOptions;

        public MacOsPasswordChangeProvider(ILogger<MacOsPasswordChangeProvider> logger, IOptions<UnixOptions> unixOptions)
        {
            this.logger = logger;
            this.unixOptions = unixOptions.Value;
            this.timeout = (int)TimeSpan.FromSeconds(Math.Max(1, this.unixOptions.DefaultCommandTimeoutSeconds)).TotalMilliseconds;
        }

        public string GetAccountName()
        {
            return string.IsNullOrWhiteSpace(unixOptions.Username) ? "root" : unixOptions.Username;
        }

        public void ChangePassword(string password)
        {
            this.ChangePasswordWithPasswd(password);
        }

        private void ChangePasswordWithPasswd(string password)
        {
            this.logger.LogTrace("Preparing command line to change password via dscl");

            List<string> arguments = new List<string> { ".", "-passwd", $"/Users/{this.GetAccountName()}", password };

            string cmdLine = "dscl";

            Process process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = cmdLine,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardInput = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                }
            };

            foreach (var arg in arguments)
            {
                process.StartInfo.ArgumentList.Add(arg);
            }

            process.Start();

            if (process.WaitForExit(timeout))
            {
                this.logger.LogTrace("Process exited");
            }
            else
            {
                process.Kill();
                this.logger.LogTrace("Process didn't exit");
            }

            if (!process.StandardOutput.EndOfStream)
            {
                this.logger.LogTrace($"Stdout: {process.StandardOutput.ReadToEnd()}");
            }

            if (!process.StandardError.EndOfStream)
            {
                this.logger.LogTrace($"Stderr: {process.StandardError.ReadToEnd()}");
            }

            if (process.ExitCode == 0)
            {
                this.logger.LogTrace("Password change was successful");
            }
            else
            {
                throw new Exception($"Password change returned error {process.ExitCode}");
            }
        }

        public void EnsureEnabled()
        {
        }
    }
}