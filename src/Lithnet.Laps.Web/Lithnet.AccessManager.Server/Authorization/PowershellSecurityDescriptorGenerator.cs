using System;
using System.IO;
using System.Management.Automation;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading.Tasks;
using Lithnet.AccessManager.Server.Configuration;
using Lithnet.AccessManager.Server.Extensions;
using Newtonsoft.Json;
using ILogger = NLog.ILogger;

namespace Lithnet.AccessManager.Server.Authorization
{
    public class PowerShellSecurityDescriptorGenerator : IPowerShellSecurityDescriptorGenerator
    {
        private readonly ILogger logger;

        private readonly IAppPathProvider env;

        private PowerShell powershell;

        public PowerShellSecurityDescriptorGenerator(ILogger logger, IAppPathProvider env)
        {
            this.logger = logger;
            this.env = env;
        }

        public CommonSecurityDescriptor GenerateSecurityDescriptor(IUser user, IComputer computer, AccessMask requestedAccess, string script, int timeout)
        {
            requestedAccess.ValidateAccessMask();
            this.InitializePowerShellSession(script);

            PowerShellAuthorizationResponse result;

            if (requestedAccess == AccessMask.Laps)
            {
                result = this.GetLapsAuthorizationResponse(user, computer, timeout);
            }
            else if (requestedAccess == AccessMask.LapsHistory)
            {
                result = this.GetLapsHistoryAuthorizationResponse(user, computer, timeout);
            }
            else if (requestedAccess == AccessMask.Jit)
            {
                result = this.GetJitAuthorizationResponse(user, computer, timeout);
            }
            else
            {
                throw new ArgumentException("The requested access type was unknown");
            }

            if (result.IsAllowed || result.IsAllowed)
            {
                DiscretionaryAcl dacl = new DiscretionaryAcl(false, false, 1);
                dacl.AddAccess(result.IsDenied ? AccessControlType.Deny : AccessControlType.Allow, user.Sid, (int)requestedAccess, InheritanceFlags.None, PropagationFlags.None);
                return new CommonSecurityDescriptor(false, false, ControlFlags.DiscretionaryAclPresent, new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null), null, null, dacl);
            }
            else
            {
                return null;
            }
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