using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Lithnet.AccessManager.Agent.Configuration;
using Lithnet.AccessManager.Agent.Linux.Configuration;
using Microsoft.Extensions.Options;

namespace Lithnet.AccessManager.Agent.Providers
{
    public class LinuxPasswordChangeProvider : IPasswordChangeProvider
    {
        private static List<string> knownChPasswdLocations = new List<string> { "/sbin/chpasswd", "/usr/sbin/chpasswd" };

        private static string chpasswdCommand;

        private readonly LinuxOptions linuxOptions;
        private readonly UnixOptions unixOptions;
        private readonly ILogger<LinuxPasswordChangeProvider> logger;
        private readonly int timeout;
        private readonly ICommandLineRunner runner;

        public LinuxPasswordChangeProvider(ILogger<LinuxPasswordChangeProvider> logger, IOptions<LinuxOptions> linuxOptions, IOptions<UnixOptions> unixOptions, ICommandLineRunner runner)
        {
            this.logger = logger;
            this.runner = runner;
            this.unixOptions = unixOptions.Value;
            this.linuxOptions = linuxOptions.Value;
            this.timeout = (int)TimeSpan.FromSeconds(Math.Max(1, this.unixOptions.DefaultCommandTimeoutSeconds)).TotalMilliseconds;
        }

        public string GetAccountName()
        {
            return string.IsNullOrWhiteSpace(unixOptions.Username) ? "root" : unixOptions.Username;
        }

        private string GetChPasswdPath()
        {
            if (chpasswdCommand != null)
            {
                return chpasswdCommand;
            }

            if (!string.IsNullOrWhiteSpace(linuxOptions.ChpasswdPath) && System.IO.File.Exists(linuxOptions.ChpasswdPath))
            {
                chpasswdCommand = linuxOptions.ChpasswdPath;
                return chpasswdCommand;
            }

            foreach (var path in knownChPasswdLocations)
            {
                if (System.IO.File.Exists(path))
                {
                    chpasswdCommand = path;
                    return chpasswdCommand;
                }
            }

            this.logger.LogTrace("Looking for chpasswd");

            var result = runner.ExecuteCommand("/bin/sh", "-c \"command -v chpasswd\"");
            result.EnsureSuccess();

            if (System.IO.File.Exists(result.StdOut))
            {
                chpasswdCommand = result.StdOut;
                return chpasswdCommand;
            }

            throw new MissingDependencyException("The chpasswd command could not be found on this system");
        }

        public void ChangePassword(string password)
        {
            this.ChangePasswordWithChpasswd(password);
        }

        //private void ChangePasswordWithPasswd(string password)
        //{
        //    this.logger.LogTrace("Preparing command line to change password via passwd");

        //    string args = linuxOptions.PasswdArgs;
        //    if(string.IsNullOrWhiteSpace(args))
        //    {
        //        args = this.GetAccountName();
        //    }
        //    else
        //    {
        //        args.Replace("{username}", this.GetAccountName(), StringComparison.OrdinalIgnoreCase);
        //    }

        //    string cmdLine = linuxOptions.PasswdPath;
        //    if( string.IsNullOrWhiteSpace(cmdLine))
        //    {
        //        cmdLine = "passwd";
        //    }

        //    Process process = new Process
        //    {
        //        StartInfo = new ProcessStartInfo
        //        {
        //            FileName = cmdLine,
        //            Arguments = args,
        //            UseShellExecute = false,
        //            CreateNoWindow = true,
        //            RedirectStandardInput = true,
        //            RedirectStandardError = true,
        //            RedirectStandardOutput = true,
        //        }
        //    };

        //    process.Start();
        //    this.logger.LogTrace("Started command line to change password");

        //    process.StandardInput.WriteLine(password);
        //    process.StandardInput.WriteLine(password);

        //    this.logger.LogTrace("Wrote new password to stdin");

        //    process.StandardInput.Close();

        //    this.logger.LogTrace("Waiting for process to exit");

        //    if (process.WaitForExit(timeout))
        //    {
        //        this.logger.LogTrace("Process exited");
        //    }
        //    else
        //    {
        //        process.Kill();
        //        this.logger.LogTrace("Process didn't exit");
        //    }

        //    if (!process.StandardOutput.EndOfStream)
        //    {
        //        this.logger.LogTrace($"Stdout: {process.StandardOutput.ReadToEnd()}");
        //    }

        //    if (!process.StandardError.EndOfStream)
        //    {
        //        this.logger.LogTrace($"Stderr: {process.StandardError.ReadToEnd()}");
        //    }


        //    if (process.ExitCode == 0)
        //    {
        //        this.logger.LogTrace("Password change was successful");
        //    }
        //    else
        //    {
        //        throw new Exception($"Password change returned error {process.ExitCode}");
        //    }
        //}

        private void ChangePasswordWithChpasswd(string password)
        {
            string chPasswd = this.GetChPasswdPath();

            Process process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = chPasswd,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardInput = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                }
            };

            process.Start();
            this.logger.LogTrace($"Changing password with {chPasswd}");

            process.StandardInput.WriteLine($"{this.GetAccountName()}:{password}");

            process.StandardInput.Close();

            if (!process.WaitForExit(this.timeout))
            {
                process.Kill();
                this.logger.LogWarning("chpasswd process didn't exit and was terminated");
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
                this.logger.LogTrace("chpasswd was successful");
            }
            else
            {
                throw new Exception($"chpasswd returned error {process.ExitCode}");
            }
        }

        public void EnsureEnabled()
        {
        }
    }
}