using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;

namespace Lithnet.AccessManager.Agent.Providers
{
    public class LinuxCommandLineRunner
    {
        private ILogger logger;

        public LinuxCommandLineRunner()
        {
        }

        public LinuxCommandLineRunner(ILogger<LinuxCommandLineRunner> logger)
        {
            this.logger = logger;
        }

        public void ExecuteCommandWithShell(string cmd)
        {
            Process process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/sh",
                    Arguments = $"-c \"{cmd}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardInput = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                }
            };

            process.Start();

            if (process.WaitForExit(5000))
            {
                if (process.ExitCode != 0)
                {
                    throw new CommandLineExecutionException($"The processed exited with error code {process.ExitCode}");
                }
            }
            else
            {
                process.Kill();
                throw new CommandLineExecutionException($"The process did not exit in the time allotted and was terminated");
            }
        }

        public bool TryExecuteCommandWithShell(string cmd)
        {
            
            try
            {
                this.ExecuteCommandWithShell(cmd);
                return true;
            }
            catch (Exception ex)
            {
                this.logger?.LogError(ex, "The process did not complete successfully");
            }

            return false;
        }

    }
}
