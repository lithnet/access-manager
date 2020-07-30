using System;
using System.Collections.Generic;
using System.Security.Principal;
using Lithnet.AccessManager.Server.Configuration;
using Lithnet.AccessManager.Server.Extensions;
using Microsoft.Extensions.Logging;

namespace Lithnet.AccessManager.Server.Authorization
{
    public class SecurityDescriptorAuthorizationService : IAuthorizationService
    {
        private readonly IDirectory directory;

        private readonly ILogger logger;

        private readonly IJitAccessGroupResolver jitResolver;

        private readonly IAuthorizationInformationBuilder authzBuilder;

        public SecurityDescriptorAuthorizationService(IDirectory directory, ILogger<SecurityDescriptorAuthorizationService> logger, IJitAccessGroupResolver jitResolver, IAuthorizationInformationBuilder authzBuilder)
        {
            this.directory = directory;
            this.logger = logger;
            this.jitResolver = jitResolver;
            this.authzBuilder = authzBuilder;
        }

        //public AuthorizationResponse GetPreAuthorization(IUser user, IComputer computer)
        //{
        //    var targets = this.GetMatchingTargetsForComputer(computer);

        //    if (targets.Count == 0)
        //    {
        //        return new PreAuthorizationResponse(AccessMask.Undefined)
        //        {
        //            Code = AuthorizationResponseCode.NoMatchingRuleForComputer,
        //        };
        //    }

        //    List<string> failureNotificationRecipients = new List<string>();
        //    List<GenericSecurityDescriptor> sds = new List<GenericSecurityDescriptor>();
        //    AuthorizationContext c = new AuthorizationContext(user.Sid, this.GetAuthorizationContextTarget(user, computer));

        //    foreach (var target in targets.Where(t => t.AuthorizationMode == AuthorizationMode.PowershellScript))
        //    {
        //        var response = this.powershell.GenerateSecurityDescriptor(user, computer, target.Script, 30);
        //        target.SecurityDescriptor = response?.GetSddlForm(AccessControlSections.All);
        //    }

        //    foreach (var target in targets)
        //    {
        //        if (string.IsNullOrWhiteSpace(target.SecurityDescriptor))
        //        {
        //            this.logger.LogTrace($"Ignoring target {target.Id} with empty security descriptor");
        //            continue;
        //        }

        //        RawSecurityDescriptor sd = new RawSecurityDescriptor(target.SecurityDescriptor);
        //        sds.Add(sd);

        //        // If the ACE did not grant any permissions to the user, consider it a failure response
        //        if (!c.AccessCheck(sd, (int)AccessMask.Laps) &&
        //            !c.AccessCheck(sd, (int)AccessMask.LapsHistory) &&
        //            !c.AccessCheck(sd, (int)AccessMask.Jit))
        //        {
        //            target.Notifications?.OnFailure?.ForEach(u => failureNotificationRecipients.Add(u));
        //        }
        //    }

        //    AccessMask allowed = 0;

        //    allowed |= c.AccessCheck(sds, (int)AccessMask.Laps) ? AccessMask.Laps : 0;
        //    allowed |= c.AccessCheck(sds, (int)AccessMask.Jit) ? AccessMask.Jit : 0;
        //    allowed |= c.AccessCheck(sds, (int)AccessMask.LapsHistory) ? AccessMask.LapsHistory : 0;

        //    if (allowed > 0)
        //    {
        //        return new PreAuthorizationResponse(allowed)
        //        {
        //            Code = AuthorizationResponseCode.Success,
        //        };
        //    }
        //    else
        //    {
        //        return new PreAuthorizationResponse(AccessMask.Undefined)
        //        {
        //            Code = AuthorizationResponseCode.NoMatchingRuleForUser,
        //            NotificationChannels = failureNotificationRecipients
        //        };
        //    }
        //}

        //public AuthorizationResponse GetAuthorizationResponse(IUser user, IComputer computer, AccessMask requestedAccess)
        //{
        //    requestedAccess.ValidateAccessMask();

        //    var targets = this.GetMatchingTargetsForComputer(computer);

        //    if (targets.Count == 0)
        //    {
        //        this.logger.LogTrace($"User {user.MsDsPrincipalName} is denied access to the password for computer {computer.MsDsPrincipalName} because the computer did not match any of the configured targets");
        //        return BuildAuthZResponseFailed(requestedAccess, AuthorizationResponseCode.NoMatchingRuleForComputer);
        //    }

        //    List<GenericSecurityDescriptor> sds = new List<GenericSecurityDescriptor>();
        //    List<string> failureNotificationRecipients = new List<string>();

        //    AuthorizationContext c = new AuthorizationContext(user.Sid, this.GetAuthorizationContextTarget(user, computer));
        //    List<SecurityDescriptorTarget> successfulTargets = new List<SecurityDescriptorTarget>();

        //    foreach (var target in targets.Where(t => t.AuthorizationMode == AuthorizationMode.PowershellScript))
        //    {
        //        var response = this.powershell.GenerateSecurityDescriptor(user, computer, target.Script, 30);
        //        target.SecurityDescriptor = response?.GetSddlForm(AccessControlSections.All);
        //    }

        //    foreach (var target in targets)
        //    {
        //        if (string.IsNullOrWhiteSpace(target.SecurityDescriptor))
        //        {
        //            this.logger.LogTrace($"Ignoring target {target.Id} with empty security descriptor");
        //            continue;
        //        }

        //        RawSecurityDescriptor sd = new RawSecurityDescriptor(target.SecurityDescriptor);
        //        sds.Add(sd);

        //        if (c.AccessCheck(sd, (int)requestedAccess))
        //        {
        //            successfulTargets.Add(target);
        //        }
        //        else
        //        {
        //            target.Notifications?.OnFailure?.ForEach(u => failureNotificationRecipients.Add(u));
        //        }
        //    }

        //    if (successfulTargets.Count == 0 || !c.AccessCheck(sds, (int)requestedAccess))
        //    {
        //        this.logger.LogTrace($"User {user.MsDsPrincipalName} is denied {requestedAccess} access for computer {computer.MsDsPrincipalName}");
        //        return BuildAuthZResponseFailed(requestedAccess, AuthorizationResponseCode.NoMatchingRuleForUser, failureNotificationRecipients);
        //    }
        //    else
        //    {
        //        var j = successfulTargets[0];
        //        this.logger.LogTrace($"User {user.MsDsPrincipalName} is authorized for {requestedAccess} access to computer {computer.MsDsPrincipalName} from target {j.Id}");

        //        return BuildAuthZResponseSuccess(requestedAccess, j, computer);
        //    }
        //}

        public AuthorizationResponse GetAuthorizationResponse(IUser user, IComputer computer, AccessMask requestedAccess)
        {
            try
            {
                requestedAccess.ValidateAccessMask();

                var info = this.authzBuilder.GetAuthorizationInformation(user, computer);

                if (info.MatchedTargets.Count == 0)
                {
                    this.logger.LogTrace($"User {user.MsDsPrincipalName} is denied access to the password for computer {computer.MsDsPrincipalName} because the computer did not match any of the configured targets");
                    return BuildAuthZResponseFailed(requestedAccess, AuthorizationResponseCode.NoMatchingRuleForComputer);
                }

                IList<SecurityDescriptorTarget> successTargets;

                if (requestedAccess.HasFlag(AccessMask.Laps))
                {
                    successTargets = info.SuccessfulLapsTargets;
                }
                else if (requestedAccess.HasFlag(AccessMask.LapsHistory))
                {
                    successTargets = info.SuccessfulLapsHistoryTargets;
                }
                else if (requestedAccess.HasFlag(AccessMask.Jit))
                {
                    successTargets = info.SuccessfulJitTargets;
                }
                else
                {
                    throw new AccessManagerException($"An invalid access mask combination was requested: {requestedAccess}");
                }

                if (successTargets.Count == 0 || !(info.EffectiveAccess.HasFlag(requestedAccess)))
                {
                    this.logger.LogTrace($"User {user.MsDsPrincipalName} is denied {requestedAccess} access for computer {computer.MsDsPrincipalName}");

                    return BuildAuthZResponseFailed(
                        requestedAccess,
                        AuthorizationResponseCode.NoMatchingRuleForUser,
                        GetNotificationRecipients(info.FailedTargets, false));
                }
                else
                {
                    var j = successTargets[0];
                    this.logger.LogTrace($"User {user.MsDsPrincipalName} is authorized for {requestedAccess} access to computer {computer.MsDsPrincipalName} from target {j.Id}");

                    return BuildAuthZResponseSuccess(requestedAccess, j, computer);
                }
            }
            finally
            {
                this.authzBuilder.ClearCache(user, computer);
            }
        }

        public AuthorizationResponse GetPreAuthorization(IUser user, IComputer computer)
        {
            var info = this.authzBuilder.GetAuthorizationInformation(user, computer);

            if (info.MatchedTargets.Count == 0)
            {
                return new PreAuthorizationResponse(AccessMask.None)
                {
                    Code = AuthorizationResponseCode.NoMatchingRuleForComputer,
                };
            }

            if (info.EffectiveAccess > 0)
            {
                return new PreAuthorizationResponse(info.EffectiveAccess)
                {
                    Code = AuthorizationResponseCode.Success,
                };
            }
            else
            {
                return new PreAuthorizationResponse(AccessMask.None)
                {
                    Code = AuthorizationResponseCode.NoMatchingRuleForUser,
                    NotificationChannels = GetNotificationRecipients(info.FailedTargets, false)
                };
            }
        }

        private static AuthorizationResponse BuildAuthZResponseFailed(AccessMask requestedAccess, AuthorizationResponseCode code, IList<string> failureNotificationRecipients = null)
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
                    AuthorizingGroup = this.jitResolver.GetJitGroup(computer, j.Jit.AuthorizingGroup).MsDsPrincipalName,
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

        private IList<string> GetNotificationRecipients(IList<SecurityDescriptorTarget> targets, bool success)
        {
            List<string> list = new List<string>();

            if (targets == null || targets.Count == 0)
            {
                return list;
            }

            foreach (var target in targets)
            {
                list.AddRange(GetNotificationRecipients(target.Notifications, success));
            }

            return list;
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