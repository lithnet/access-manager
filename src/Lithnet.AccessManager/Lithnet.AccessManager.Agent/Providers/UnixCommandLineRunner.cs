using Lithnet.AccessManager.Agent.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Diagnostics;
using System.Linq;

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

        public CommandLineResult ExecuteCommand(string cmd, params string[] args)
        {
            return this.ExecuteCommand(cmd, this.defaultTimeout, args);
        }

        public CommandLineResult ExecuteCommand(string cmd, TimeSpan timeout, params string[] args)
        {
            Process process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = cmd,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardInput = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                }
            };

#if NETCOREAPP
            if (args != null)
            {
                foreach (var arg in args)
                {
                    process.StartInfo.ArgumentList.Add(arg);
                }
            }
#else
            if (args != null || args.Any())
            {
                throw new NotSupportedException();
            }
#endif

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
    }
}