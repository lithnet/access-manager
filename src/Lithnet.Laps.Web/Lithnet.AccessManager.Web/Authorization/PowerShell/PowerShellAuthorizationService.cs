using System;
using System.IO;
using System.Management.Automation;
using System.Threading.Tasks;
using Lithnet.AccessManager.Configuration;
using Lithnet.AccessManager.Web.AppSettings;
using Lithnet.AccessManager.Web.Internal;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NLog;

namespace Lithnet.AccessManager.Web.Authorization
{
    public class PowershellAuthorizationService : IAuthorizationService
    {
        private readonly ILogger logger;

        private readonly PowershellAuthorizationProviderOptions options;

        private readonly IWebHostEnvironment env;

        private PowerShell powershell;

        public PowershellAuthorizationService(ILogger logger, IOptions<PowershellAuthorizationProviderOptions> options, IWebHostEnvironment env)
        {
            this.logger = logger;
            this.options = options.Value;
            this.env = env;
        }

        public AuthorizationResponse GetAuthorizationResponse(IUser user, IComputer computer, AccessMask requestedAccess)
        {
            requestedAccess.ValidateAccessMask();

            if (requestedAccess == AccessMask.Laps)
            {
                return this.GetLapsAuthorizationResponse(user, computer);
            }
            else if  (requestedAccess == AccessMask.LapsHistory)
            {
                return this.GetLapsHistoryAuthorizationResponse(user, computer);
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
            if (!task.Wait(TimeSpan.FromSeconds(this.options.ScriptTimeout)))
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
            if (!task.Wait(TimeSpan.FromSeconds(this.options.ScriptTimeout)))
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

        private LapsHistoryAuthorizationResponse GetLapsHistoryAuthorizationResponse(IUser user, IComputer computer)
        {
            if (powershell == null)
            {
                this.InitializePowerShellSession();
            }

            this.powershell.ResetState();
            this.powershell
                .AddCommand("Get-LapsHistoryAuthorizationResponse")
                    .AddParameter("user", user)
                    .AddParameter("computer", computer)
                    .AddParameter("logger", logger);

            Task<LapsHistoryAuthorizationResponse> task = new Task<LapsHistoryAuthorizationResponse>(() =>
            {
                var results = this.powershell.Invoke();
                this.powershell.ThrowOnPipelineError();

                foreach (PSObject result in results)
                {
                    if (result.BaseObject is LapsHistoryAuthorizationResponse res)
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
            if (!task.Wait(TimeSpan.FromSeconds(this.options.ScriptTimeout)))
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

            return new LapsHistoryAuthorizationResponse()
            {
                Code = AuthorizationResponseCode.NoMatchingRuleForComputer,
            };
        }


        private void InitializePowerShellSession()
        {
            string path = this.env.ResolvePath(this.options.ScriptFile);

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