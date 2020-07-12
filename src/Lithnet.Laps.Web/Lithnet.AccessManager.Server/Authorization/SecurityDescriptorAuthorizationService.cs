using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using Lithnet.AccessManager.Server.Auditing;
using Lithnet.AccessManager.Server.Configuration;
using Lithnet.AccessManager.Server.Extensions;
using Lithnet.Security.Authorization;
using Microsoft.Extensions.Options;
using NLog;

namespace Lithnet.AccessManager.Server.Authorization
{
    public class SecurityDescriptorAuthorizationService : IAuthorizationService
    {
        private readonly IDirectory directory;

        private readonly ILogger logger;

        private readonly BuiltInProviderOptions options;

        private readonly IJitAccessGroupResolver jitResolver;

        private readonly IPowerShellSecurityDescriptorGenerator powershell;

        public SecurityDescriptorAuthorizationService(IOptions<BuiltInProviderOptions> options, IDirectory directory, ILogger logger, IJitAccessGroupResolver jitResolver, IPowerShellSecurityDescriptorGenerator powershell)
        {
            this.directory = directory;
            this.logger = logger;
            this.options = options.Value;
            this.jitResolver = jitResolver;
            this.powershell = powershell;
        }

        public AuthorizationResponse GetAuthorizationResponse(IUser user, IComputer computer, AccessMask requestedAccess)
        {
            requestedAccess.ValidateAccessMask();

            var targets = this.GetMatchingTargetsForComputer(computer);

            if (targets.Count == 0)
            {
                this.logger.Trace($"User {user.MsDsPrincipalName} is denied access to the password for computer {computer.MsDsPrincipalName} because the computer did not match any of the configured targets");
                return BuildAuthZResponseFailed(requestedAccess, AuthorizationResponseCode.NoMatchingRuleForComputer);
            }

            List<GenericSecurityDescriptor> sds = new List<GenericSecurityDescriptor>();
            List<string> failureNotificationRecipients = new List<string>();

            AuthorizationContext c = new AuthorizationContext(user.Sid, this.GetAuthorizationContextTarget(user, computer));
            List<SecurityDescriptorTarget> successfulTargets = new List<SecurityDescriptorTarget>();

            foreach (var target in targets.Where(t => t.AuthorizationMode == AuthorizationMode.PowershellScript))
            {
                var response = this.powershell.GenerateSecurityDescriptor(user, computer, requestedAccess, target.Script, 30);
                target.SecurityDescriptor = response?.GetSddlForm(AccessControlSections.All);
            }

            foreach (var target in targets)
            {
                if (string.IsNullOrWhiteSpace(target.SecurityDescriptor))
                {
                    this.logger.Trace($"Ignoring target {target.Id} with empty security descriptor");
                    continue;
                }

                RawSecurityDescriptor sd = new RawSecurityDescriptor(target.SecurityDescriptor);
                sds.Add(sd);

                if (c.AccessCheck(sd, (int)requestedAccess))
                {
                    successfulTargets.Add(target);
                }
                else
                {
                    target.Notifications?.OnFailure?.ForEach(u => failureNotificationRecipients.Add(u));
                }
            }

            if (successfulTargets.Count == 0 || !c.AccessCheck(sds, (int)requestedAccess))
            {
                this.logger.Trace($"User {user.MsDsPrincipalName} is denied {requestedAccess} access for computer {computer.MsDsPrincipalName}");
                return BuildAuthZResponseFailed(requestedAccess, AuthorizationResponseCode.NoMatchingRuleForUser, failureNotificationRecipients);
            }
            else
            {
                var j = successfulTargets[0];
                this.logger.Trace($"User {user.MsDsPrincipalName} is authorized for {requestedAccess} access to computer {computer.MsDsPrincipalName} from target {j.Id}");

                return BuildAuthZResponseSuccess(requestedAccess, j, computer);
            }
        }

        private string GetAuthorizationContextTarget(IUser user, IComputer computer)
        {
            switch (this.options.AccessControlEvaluationLocation)
            {
                case AclEvaluationLocation.ComputerDomain:
                    return this.directory.GetDnsDomainName(computer.Sid);

                case AclEvaluationLocation.UserDomain:
                    return this.directory.GetDnsDomainName(user.Sid);

                default:
                    return null;
            }
        }

        private static AuthorizationResponse BuildAuthZResponseFailed(AccessMask requestedAccess, AuthorizationResponseCode code, List<string> failureNotificationRecipients = null)
        {
            AuthorizationResponse response = AuthorizationResponse.CreateAuthorizationResponse(requestedAccess);
            response.Code = code;
            response.NotificationChannels = failureNotificationRecipients;

            return response;
        }

        private AuthorizationResponse BuildAuthZResponseSuccess(AccessMask requestedAccess, SecurityDescriptorTarget j, IComputer computer)
        {
            AuthorizationResponse response;

            if (requestedAccess == AccessMask.Laps)
            {
                response = new LapsAuthorizationResponse()
                {
                    ExpireAfter = j.Laps.ExpireAfter,
                    RetrievalLocation = j.Laps.RetrievalLocation
                };
            }
            else if (requestedAccess == AccessMask.LapsHistory)
            {
                response = new LapsHistoryAuthorizationResponse();
            }
            else if (requestedAccess == AccessMask.Jit)
            {
                response = new JitAuthorizationResponse()
                {
                    ExpireAfter = j.Jit.ExpireAfter,
                    AuthorizingGroup = this.jitResolver.GetJitAccessGroup(computer, j.Jit?.AuthorizingGroup).MsDsPrincipalName
                };
            }
            else
            {
                throw new AccessManagerException("An invalid access mask was requested");
            }

            response.MatchedRule = j.Id;
            response.MatchedRuleDescription = j.Description ?? $"{j.Type}: {this.TryGetNameIfSid(j.Target)}";
            response.Code = AuthorizationResponseCode.Success;
            response.NotificationChannels = this.GetNotificationRecipients(j.Notifications, true);
            return response;
        }

        private IList<string> GetNotificationRecipients(AuditNotificationChannels t, bool success)
        {
            List<string> list = new List<string>();

            if (success)
            {
                t?.OnSuccess?.ForEach(u => list.Add(u));
            }
            else
            {
                t?.OnFailure?.ForEach(u => list.Add(u));
            }

            return list;
        }


        private IList<SecurityDescriptorTarget> GetMatchingTargetsForComputer(IComputer computer)
        {
            List<SecurityDescriptorTarget> matchingTargets = new List<SecurityDescriptorTarget>();

            foreach (var target in this.options.Targets.OrderBy(t => (int)t.Type))
            {
                try
                {
                    if (target.Type == TargetType.Container)
                    {
                        if (this.directory.IsObjectInOu(computer, target.Target))
                        {
                            this.logger.Trace($"Matched {computer.MsDsPrincipalName} to target OU {target.Target}");
                            matchingTargets.Add(target);
                        }
                    }
                    else if (target.Type == TargetType.Computer)
                    {
                        IComputer p;
                        try
                        {
                            p = this.directory.GetComputer(target.GetTargetAsSid());
                        }
                        catch (ObjectNotFoundException ex)
                        {
                            this.logger.Trace(ex, $"Target computer {target.Target} was not found in the directory");
                            continue;
                        }

                        if (p.Sid == computer.Sid)
                        {
                            this.logger.Trace($"Matched {computer.MsDsPrincipalName} to target {target.Id}");
                            matchingTargets.Add(target);
                        }
                    }
                    else
                    {
                        IGroup g;
                        try
                        {
                            g = this.directory.GetGroup(target.GetTargetAsSid());
                        }
                        catch (ObjectNotFoundException ex)
                        {
                            this.logger.Trace(ex, $"Target group {target.Target} was not found in the directory");
                            continue;
                        }

                        if (this.directory.IsSidInPrincipalToken(g.Sid, computer, computer.Sid))
                        {
                            this.logger.Trace($"Matched {computer.MsDsPrincipalName} to target {target.Id}");
                            matchingTargets.Add(target);
                        }
                    }
                }
                catch (Exception ex)
                {
                    this.logger.LogEventError(EventIDs.TargetRuleProcessingError, $"An error occurred processing the target {target.Id}:{target.Type}:{target.Target}", ex);
                }
            }

            return matchingTargets;
        }

        private string TryGetNameIfSid(string sid)
        {
            if (string.IsNullOrWhiteSpace(sid))
            {
                return null;
            }

            try
            {
                SecurityIdentifier s = new SecurityIdentifier(sid);
                if (this.directory.TryGetPrincipal(sid, out ISecurityPrincipal principal))
                {
                    return principal.MsDsPrincipalName;
                }
                else
                {
                    return sid;
                }
            }
            catch (Exception)
            {
                return sid;
            }
        }
    }
}