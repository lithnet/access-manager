using System;
using System.IO;
using System.Management.Automation;
using System.Threading.Tasks;
using Lithnet.AccessManager.Server;
using Lithnet.AccessManager.Web.Internal;
using Newtonsoft.Json;
using ILogger = NLog.ILogger;

namespace Lithnet.AccessManager.Web.Authorization
{
    public class PowershellAuthorizationService
    {
        private readonly ILogger logger;

        private readonly IAppPathProvider env;

        private PowerShell powershell;

        public PowershellAuthorizationService(ILogger logger, IAppPathProvider env)
        {
            this.logger = logger;
            this.env = env;
        }

        public PowerShellAuthorizationResponse GetAuthorizationResponse(IUser user, IComputer computer, AccessMask requestedAccess, string script, int timeout)
        {
            requestedAccess.ValidateAccessMask();
            this.InitializePowerShellSession(script);

            if (requestedAccess == AccessMask.Laps)
            {
                return this.GetLapsAuthorizationResponse(user, computer, timeout);
            }
            else if (requestedAccess == AccessMask.LapsHistory)
            {
                return this.GetLapsHistoryAuthorizationResponse(user, computer, timeout);
            }
            else if (requestedAccess == AccessMask.Jit)
            {
                return this.GetJitAuthorizationResponse(user, computer, timeout);
            }

            throw new ArgumentException("The requested access type was unknown");
        }

        private PowerShellAuthorizationResponse GetJitAuthorizationResponse(IUser user, IComputer computer, int timeout)
        {
            this.powershell.ResetState();
            this.powershell
                .AddCommand("Get-JitAuthorizationResponse")
                    .AddParameter("user", user)
                    .AddParameter("computer", computer)
                    .AddParameter("logger", logger);

            Task<PowerShellAuthorizationResponse> task = new Task<PowerShellAuthorizationResponse>(() =>
            {
                var results = this.powershell.Invoke();
                this.powershell.ThrowOnPipelineError();

                foreach (PSObject result in results)
                {
                    if (result.BaseObject is PowerShellAuthorizationResponse res)
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
            if (!task.Wait(TimeSpan.FromSeconds(timeout)))
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

            return new PowerShellAuthorizationResponse();
        }

        private PowerShellAuthorizationResponse GetLapsAuthorizationResponse(IUser user, IComputer computer, int timeout)
        {
            this.powershell.ResetState();
            this.powershell
                .AddCommand("Get-LapsAuthorizationResponse")
                    .AddParameter("user", user)
                    .AddParameter("computer", computer)
                    .AddParameter("logger", logger);

            Task<PowerShellAuthorizationResponse> task = new Task<PowerShellAuthorizationResponse>(() =>
            {
                var results = this.powershell.Invoke();
                this.powershell.ThrowOnPipelineError();

                foreach (PSObject result in results)
                {
                    if (result.BaseObject is PowerShellAuthorizationResponse res)
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
            if (!task.Wait(TimeSpan.FromSeconds(timeout)))
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

            return new PowerShellAuthorizationResponse();
        }

        private PowerShellAuthorizationResponse GetLapsHistoryAuthorizationResponse(IUser user, IComputer computer, int timeout)
        {
            this.powershell.ResetState();
            this.powershell
                .AddCommand("Get-LapsHistoryAuthorizationResponse")
                    .AddParameter("user", user)
                    .AddParameter("computer", computer)
                    .AddParameter("logger", logger);

            Task<PowerShellAuthorizationResponse> task = new Task<PowerShellAuthorizationResponse>(() =>
            {
                var results = this.powershell.Invoke();
                this.powershell.ThrowOnPipelineError();

                foreach (PSObject result in results)
                {
                    if (result.BaseObject is PowerShellAuthorizationResponse res)
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
            if (!task.Wait(TimeSpan.FromSeconds(timeout)))
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

            return new PowerShellAuthorizationResponse()
          ;
        }

        private void InitializePowerShellSession(string script)
        {
            string path = this.env.GetFullPath(script, this.env.ScriptsPath);

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

            if (powershell.Runspace.SessionStateProxy.InvokeCommand.GetCommand("Get-LapsHistoryAuthorizationResponse", CommandTypes.All) == null)
            {
                throw new NotSupportedException("The PowerShell script must contain a function called 'Get-LapsHistoryAuthorizationResponse'");
            }

            if (powershell.Runspace.SessionStateProxy.InvokeCommand.GetCommand("Get-JitAuthorizationResponse", CommandTypes.All) == null)
            {
                throw new NotSupportedException("The PowerShell script must contain a function called 'Get-JitAuthorizationResponse'");
            }

            this.logger.Trace($"The PowerShell script was successfully initialized");
        }
    }
}