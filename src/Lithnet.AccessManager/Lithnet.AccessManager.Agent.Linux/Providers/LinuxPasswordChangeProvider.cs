using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using Lithnet.AccessManager.Agent.Configuration;
using Lithnet.AccessManager.Agent.Linux.Configuration;
using Microsoft.Extensions.Options;

namespace Lithnet.AccessManager.Agent.Providers
{
    public class LinuxPasswordChangeProvider : IPasswordChangeProvider
    {
        private static bool hasChpasswd;
        private static bool hasCheckedChpasswd;
        private static string chpasswdCommand;

        private readonly LinuxOptions linuxOptions;
        private readonly UnixOptions unixOptions;
        private readonly ILogger<LinuxPasswordChangeProvider> logger;
        private readonly int timeout;

        public LinuxPasswordChangeProvider(ILogger<LinuxPasswordChangeProvider> logger, IOptions<LinuxOptions> linuxOptions, IOptions<UnixOptions> unixOptions)
        {
            this.logger = logger;
            this.unixOptions = unixOptions.Value;
            this.linuxOptions = linuxOptions.Value;
            this.timeout = (int)TimeSpan.FromSeconds(Math.Max(1, this.unixOptions.DefaultCommandTimeoutSeconds)).TotalMilliseconds;
        }

        public string GetAccountName()
        {
            return string.IsNullOrWhiteSpace(unixOptions.Username) ? "root" : unixOptions.Username;
        }

        private bool HasChangePasswordCommand()
        {
            if (hasCheckedChpasswd)
            {
                return hasChpasswd;
            }

            if (linuxOptions.DisableChpasswd ?? false)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(linuxOptions.ChpasswdPath))
            {
                this.logger.LogTrace($"Using chpasswd from config file {linuxOptions.ChpasswdPath}");

                hasCheckedChpasswd = true;
                hasChpasswd = true;
                chpasswdCommand = linuxOptions.ChpasswdPath;
                return true;
            }

            this.logger.LogTrace("Looking for chpasswd");
            Process process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/sh",
                    Arguments = "-c \"command -v chpasswd\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardInput = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                }
            };

            process.Start();

            hasChpasswd = false;

            if (process.WaitForExit(timeout))
            {
                if (process.ExitCode == 0)
                {
                    chpasswdCommand = process.StandardOutput.ReadToEnd().Trim();
                    this.logger.LogTrace($"chpasswd found at {chpasswdCommand}");
                    hasChpasswd = true;
                }
                else
                {
                    this.logger.LogTrace($"command lookup returned exit code {process.ExitCode}");
                }
            }
            else
            {
                process.Kill();
            }

            hasCheckedChpasswd = true;
            return hasChpasswd;
        }

        public void ChangePassword(string password)
        {
            if (this.HasChangePasswordCommand())
            {
                this.ChangePasswordWithChpasswd(password);
            }
            else
            {
                this.ChangePasswordWithPasswd(password);
            }
        }

        private void ChangePasswordWithPasswd(string password)
        {
            this.logger.LogTrace("Preparing command line to change password via passwd");

            string args = linuxOptions.PasswdArgs;
            if(string.IsNullOrWhiteSpace(args))
            {
                args = this.GetAccountName();
            }
            else
            {
                args.Replace("{username}", this.GetAccountName(), StringComparison.OrdinalIgnoreCase);
            }

            string cmdLine = linuxOptions.PasswdPath;
            if( string.IsNullOrWhiteSpace(cmdLine))
            {
                cmdLine = "passwd";
            }

            Process process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = cmdLine,
                    Arguments = args,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardInput = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                }
            };

            process.Start();
            this.logger.LogTrace("Started command line to change password");

            process.StandardInput.WriteLine(password);
            process.StandardInput.WriteLine(password);

            this.logger.LogTrace("Wrote new password to stdin");

            process.StandardInput.Close();

            this.logger.LogTrace("Waiting for process to exit");

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
        private void ChangePasswordWithChpasswd(string password)
        {
            this.logger.LogTrace("Preparing command line to change password via chpasswd");

            string args = this.linuxOptions.ChpasswdArgs ?? string.Empty;

            Process process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = chpasswdCommand,
                    Arguments = args,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardInput = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                }
            };

            process.Start();
            this.logger.LogTrace("Started command line to change password");

            process.StandardInput.WriteLine($"{this.GetAccountName()}:{password}");

            this.logger.LogTrace("Wrote new password to stdin");

            process.StandardInput.Close();

            this.logger.LogTrace("Waiting for process to exit");

            if (process.WaitForExit(this.timeout))
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