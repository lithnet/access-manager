using System;
using NLog;
using System.Management.Automation;
using System.IO;
using Lithnet.Laps.Web.AppSettings;
using System.Threading.Tasks;
using Lithnet.Laps.Web.ActiveDirectory;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Hosting;
using Lithnet.Laps.Web.Internal;

namespace Lithnet.Laps.Web.Authorization
{
    public class PowershellAuthorizationService : IAuthorizationService
    {
        private readonly ILogger logger;

        private readonly IAuthorizationSettings config;

        private readonly IWebHostEnvironment env;

        private PowerShell powershell;

        public PowershellAuthorizationService(ILogger logger, IAuthorizationSettings config, IWebHostEnvironment env)
        {
            this.logger = logger;
            this.config = config;
            this.env = env;
        }

        public AuthorizationResponse GetAuthorizationResponse(IUser user, IComputer computer, AccessMask requestedAccess)
        {
            if (requestedAccess == AccessMask.Laps)
            {
                return this.GetLapsAuthorizationResponse(user, computer);
            }
            else if (requestedAccess == AccessMask.Jit)
            {
                return this.GetJitAuthorizationResponse(user, computer);
            }

            throw new ArgumentException("The requested access type was unknown");
        }

        private JitAuthorizationResponse GetJitAuthorizationResponse(IUser user, IComputer computer)
        {
            if (powershell == null)
            {
                this.InitializePowerShellSession();
            }

            this.powershell.ResetState();
            this.powershell
                .AddCommand("Get-JitAuthorizationResponse")
                    .AddParameter("user", user)
                    .AddParameter("computer", computer)
                    .AddParameter("logger", logger);

            Task<JitAuthorizationResponse> task = new Task<JitAuthorizationResponse>(() =>
            {
                var results = this.powershell.Invoke();
                this.powershell.ThrowOnPipelineError();

                foreach (PSObject result in results)
                {
                    if (result.BaseObject is JitAuthorizationResponse res)
                    {
                        return res;
                    }
                    else
                    {
                        this.logger.Warn($"The powerShell script returned an unsupported object of type {result.BaseObject?.GetType().FullName} to the pipeline");
                    }
                }

                return null;
            });

            task.Start();
            if (!task.Wait(TimeSpan.FromSeconds(this.config.PowershellScriptTimeout)))
            {
                throw new TimeoutException("The PowerShell script did not complete within the configured time");
            }

            if (task.IsFaulted)
            {
                throw task.Exception;
            }

            if (task.Result != null)
            {
                this.logger.Trace($"PowerShell script returned the following AuthorizationResponse: {JsonConvert.SerializeObject(task.Result)}");
                return task.Result;
            }

            this.logger.Warn($"The PowerShell script did not return an AuthorizationResponse");

            return new JitAuthorizationResponse()
            {
                Code = AuthorizationResponseCode.NoMatchingRuleForComputer,
            };
        }

        private LapsAuthorizationResponse GetLapsAuthorizationResponse(IUser user, IComputer computer)
        {
            if (powershell == null)
            {
                this.InitializePowerShellSession();
            }

            this.powershell.ResetState();
            this.powershell
                .AddCommand("Get-LapsAuthorizationResponse")
                    .AddParameter("user", user)
                    .AddParameter("computer", computer)
                    .AddParameter("logger", logger);

            Task<LapsAuthorizationResponse> task = new Task<LapsAuthorizationResponse>(() =>
            {
                var results = this.powershell.Invoke();
                this.powershell.ThrowOnPipelineError();

                foreach (PSObject result in results)
                {
                    if (result.BaseObject is LapsAuthorizationResponse res)
                    {
                        return res;
                    }
                    else
                    {
                        this.logger.Warn($"The powerShell script returned an unsupported object of type {result.BaseObject?.GetType().FullName} to the pipeline");
                    }
                }

                return null;
            });

            task.Start();
            if (!task.Wait(TimeSpan.FromSeconds(this.config.PowershellScriptTimeout)))
            {
                throw new TimeoutException("The PowerShell script did not complete within the configured time");
            }

            if (task.IsFaulted)
            {
                throw task.Exception;
            }

            if (task.Result != null)
            {
                this.logger.Trace($"PowerShell script returned the following AuthorizationResponse: {JsonConvert.SerializeObject(task.Result)}");
                return task.Result;
            }

            this.logger.Warn($"The PowerShell script did not return an AuthorizationResponse");

            return new LapsAuthorizationResponse()
            {
                Code = AuthorizationResponseCode.NoMatchingRuleForComputer,
            };
        }

        private void InitializePowerShellSession()
        {
            string path = this.env.ResolvePath(this.config.PowershellScriptFile);

            if (path == null || !File.Exists(path))
            {
                throw new FileNotFoundException($"The PowerShell script was not found: {path}");
            }

            powershell = PowerShell.Create();
            powershell.AddScript(File.ReadAllText(path));
            powershell.Invoke();

            if (powershell.Runspace.SessionStateProxy.InvokeCommand.GetCommand("Get-LapsAuthorizationResponse", CommandTypes.All) == null)
            {
                throw new NotSupportedException("The PowerShell script must contain a function called 'Get-LapsAuthorizationResponse'");
            }

            this.logger.Trace($"The PowerShell script was successfully initialized");
        }
    }
}