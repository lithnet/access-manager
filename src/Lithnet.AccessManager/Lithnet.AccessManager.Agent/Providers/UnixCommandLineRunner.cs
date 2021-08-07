using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using Lithnet.AccessManager.Agent.Configuration;
using Microsoft.Extensions.Options;

namespace Lithnet.AccessManager.Agent.Providers
{
    public class UnixCommandLineRunner : ICommandLineRunner
    {
        private ILogger logger;
        private readonly UnixOptions options;
        private readonly TimeSpan defaultTimeout;

        public UnixCommandLineRunner()
        {
            this.options = new UnixOptions();
            this.defaultTimeout = TimeSpan.FromSeconds(this.options.DefaultCommandTimeoutSeconds);
        }

        public UnixCommandLineRunner(IOptions<UnixOptions> options)
        {
            this.options = options.Value;
            this.defaultTimeout = TimeSpan.FromSeconds(Math.Max(1, this.options.DefaultCommandTimeoutSeconds));
        }

        public UnixCommandLineRunner(ILogger<UnixCommandLineRunner> logger, IOptions<UnixOptions> options)
        : this(options)
        {
            this.logger = logger;
        }

        public CommandLineResult ExecuteCommand(string cmd, string args)
        {
            return this.ExecuteCommand(cmd, args, this.defaultTimeout);
        }

        public CommandLineResult ExecuteCommand(string cmd, string args, TimeSpan timeout)
        {
            Process process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = cmd,
                    Arguments = args,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardInput = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                }
            };

            process.Start();

            if (process.WaitForExit((int)timeout.TotalMilliseconds))
            {
                return new CommandLineResult
                {
                    StdErr = process.StandardError.ReadToEnd(),
                    StdOut = process.StandardOutput.ReadToEnd(),
                    ExitCode = process.ExitCode
                };
            }
            else
            {
                process.Kill();

                return new CommandLineResult
                {
                    StdErr = process.StandardError.ReadToEnd(),
                    StdOut = process.StandardOutput.ReadToEnd(),
                    ExitCode = process.ExitCode,
                    Timeout = true,
                };
            }
        }
        public CommandLineResult ExecuteCommandWithDefaultShell(string cmd)
        {
            return this.ExecuteCommandWithDefaultShell(cmd, this.defaultTimeout);
        }

        public CommandLineResult ExecuteCommandWithDefaultShell(string cmd, TimeSpan timeout)
        {
            return this.ExecuteCommand(this.options.DefaultShell, $"-c \"{cmd}\"", timeout);

        }

        public CommandLineResult ExecuteCommandWithDefaultShell(string cmd, string args, TimeSpan timeout)
        {
            return this.ExecuteCommand(this.options.DefaultShell, $"-c \"{cmd} {args}\"", timeout);
        }

        public CommandLineResult ExecuteCommandWithDefaultShell(string cmd, string args)
        {
            return this.ExecuteCommandWithDefaultShell(cmd, args, this.defaultTimeout);
        }

        public bool TryExecuteCommandWithShell(string cmd, out CommandLineResult result)
        {
            try
            {
                result = this.ExecuteCommandWithDefaultShell(cmd);
                return true;
            }
            catch (Exception ex)
            {
                this.logger?.LogError(ex, "The process did not complete successfully");
            }

            result = null;
            return false;
        }
    }
}
