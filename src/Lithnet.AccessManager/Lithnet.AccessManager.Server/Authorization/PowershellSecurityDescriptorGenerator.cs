using System;
using System.IO;
using System.Management.Automation;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading.Tasks;
using Lithnet.AccessManager.Server.Configuration;
using Lithnet.AccessManager.Server.Extensions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Lithnet.AccessManager.Server.Authorization
{
    public class PowerShellSecurityDescriptorGenerator : IPowerShellSecurityDescriptorGenerator
    {
        private readonly ILogger logger;

        private readonly NLog.ILogger nlogger = NLog.LogManager.GetCurrentClassLogger();

        private readonly IPowerShellSessionProvider sessionProvider;

        public PowerShellSecurityDescriptorGenerator(ILogger<PowerShellSecurityDescriptorGenerator> logger, IPowerShellSessionProvider sessionProvider)
        {
            this.sessionProvider = sessionProvider;
            this.logger = logger;
        }

        public CommonSecurityDescriptor GenerateSecurityDescriptor(IUser user, IComputer computer, string script, int timeout)
        {
            PowerShellAuthorizationResponse result = this.GetAuthorizationResponse(script, user, computer, timeout);
            return GenerateSecurityDescriptor(user.Sid, result);
        }

        public CommonSecurityDescriptor GenerateSecurityDescriptor(SecurityIdentifier sid, PowerShellAuthorizationResponse result)
        {
            AccessMask allowedAccess = 0;
            AccessMask deniedAccess = 0;

            if (result.IsLocalAdminPasswordAllowed)
            {
                allowedAccess |= AccessMask.Laps;
            }

            if (result.IsLocalAdminPasswordHistoryAllowed)
            {
                allowedAccess |= AccessMask.LapsHistory;
            }

            if (result.IsJitAllowed)
            {
                allowedAccess |= AccessMask.Jit;
            }

            if (result.IsLocalAdminPasswordDenied)
            {
                deniedAccess |= AccessMask.Laps;
            }

            if (result.IsLocalAdminPasswordHistoryDenied)
            {
                deniedAccess |= AccessMask.LapsHistory;
            }

            if (result.IsJitDenied)
            {
                deniedAccess |= AccessMask.Jit;
            }

            DiscretionaryAcl dacl;

            if (allowedAccess > 0 && deniedAccess > 0)
            {
                dacl = new DiscretionaryAcl(false, false, 2);
            }
            else if (allowedAccess > 0 || deniedAccess > 0)
            {
                dacl = new DiscretionaryAcl(false, false, 1);
            }
            else
            {
                dacl = new DiscretionaryAcl(false, false, 0);
            }

            if (allowedAccess > 0)
            {
                dacl.AddAccess(AccessControlType.Allow, sid, (int) allowedAccess, InheritanceFlags.None, PropagationFlags.None);
            }

            if (deniedAccess > 0)
            {
                dacl.AddAccess(AccessControlType.Deny, sid, (int) deniedAccess, InheritanceFlags.None, PropagationFlags.None);
            }

            return new CommonSecurityDescriptor(false, false, ControlFlags.DiscretionaryAclPresent, new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null), null, null, dacl);
        }

        private PowerShellAuthorizationResponse GetAuthorizationResponse(string script, IUser user, IComputer computer, int timeout)
        {
            PowerShell powershell = this.sessionProvider.GetSession(script, "Get-AuthorizationResponse");
            powershell.AddCommand("Get-AuthorizationResponse")
                .AddParameter("user", user)
                .AddParameter("computer", computer)
                .AddParameter("logger", nlogger);

            Task<PowerShellAuthorizationResponse> task = new Task<PowerShellAuthorizationResponse>(() =>
            {
                var results = powershell.Invoke();
                powershell.ThrowOnPipelineError();

                foreach (PSObject result in results)
                {
                    if (result.BaseObject is PowerShellAuthorizationResponse res)
                    {
                        return res;
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
                if (task.Exception != null) throw task.Exception;
                throw new AccessManagerException("The task failed");
            }

            if (task.Result != null)
            {
                this.logger.LogTrace($"PowerShell script returned the following AuthorizationResponse: {JsonConvert.SerializeObject(task.Result)}");
                return task.Result;
            }

            this.logger.LogWarning(EventIDs.PowerShellSDGeneratorInvalidResponse, $"The PowerShell script did not return an AuthorizationResponse");

            return new PowerShellAuthorizationResponse();
        }
    }
}