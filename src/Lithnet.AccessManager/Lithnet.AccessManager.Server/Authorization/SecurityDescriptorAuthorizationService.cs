using System;
using System.Collections.Generic;
using System.Security.Principal;
using Lithnet.AccessManager.Server.Configuration;
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

        public AuthorizationResponse GetAuthorizationResponse(IUser user, IComputer computer, AccessMask requestedAccess)
        {
            try
            {
                requestedAccess.ValidateAccessMask();

                var info = this.authzBuilder.GetAuthorizationInformation(user, computer);

                if (info.MatchedComputerTargets.Count == 0)
                {
                    this.logger.LogTrace($"User {user.MsDsPrincipalName} is denied access to the password for computer {computer.MsDsPrincipalName} because the computer did not match any of the configured targets");
                    return BuildAuthZResponseFailed(requestedAccess, AuthorizationResponseCode.NoMatchingRuleForComputer);
                }

                IList<SecurityDescriptorTarget> successTargets;

                if (requestedAccess.HasFlag(AccessMask.LocalAdminPassword))
                {
                    successTargets = info.SuccessfulLapsTargets;
                }
                else if (requestedAccess.HasFlag(AccessMask.LocalAdminPasswordHistory))
                {
                    successTargets = info.SuccessfulLapsHistoryTargets;
                }
                else if (requestedAccess.HasFlag(AccessMask.Jit))
                {
                    successTargets = info.SuccessfulJitTargets;
                }
                else if (requestedAccess.HasFlag(AccessMask.BitLocker))
                {
                    successTargets = info.SuccessfulBitLockerTargets;
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
                    var matchedTarget = successTargets[0];
                    this.logger.LogTrace($"User {user.MsDsPrincipalName} is authorized for {requestedAccess} access to computer {computer.MsDsPrincipalName} from target {matchedTarget.Id}");

                    return BuildAuthZResponseSuccess(requestedAccess, matchedTarget, computer);
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

            if (info.MatchedComputerTargets.Count == 0)
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

        private AuthorizationResponse BuildAuthZResponseSuccess(AccessMask requestedAccess, SecurityDescriptorTarget matchedTarget, IComputer computer)
        {
            AuthorizationResponse response;

            if (requestedAccess == AccessMask.LocalAdminPassword)
            {
                response = new LapsAuthorizationResponse()
                {
                    ExpireAfter = matchedTarget.Laps.ExpireAfter,
                    RetrievalLocation = matchedTarget.Laps.RetrievalLocation
                };
            }
            else if (requestedAccess == AccessMask.LocalAdminPasswordHistory)
            {
                response = new LapsHistoryAuthorizationResponse();
            }
            else if (requestedAccess == AccessMask.Jit)
            {
                if (string.IsNullOrWhiteSpace(matchedTarget.Jit.AuthorizingGroup))
                {
                    throw new ConfigurationException($"The target {matchedTarget.Id} has an empty JIT group");
                }

                response = new JitAuthorizationResponse()
                {
                    ExpireAfter = matchedTarget.Jit.ExpireAfter,
                    AuthorizingGroup = this.jitResolver.GetJitGroup(computer, matchedTarget.Jit.AuthorizingGroup).MsDsPrincipalName,
                };
            }
            else if (requestedAccess == AccessMask.BitLocker)
            {
                response = new BitLockerAuthorizationResponse();
            }
            else
            {
                throw new AccessManagerException("An invalid access mask was requested");
            }

            response.MatchedRule = matchedTarget.Id;
            response.MatchedRuleDescription = matchedTarget.Description ?? $"{matchedTarget.Type}: {this.TryGetNameIfSid(matchedTarget.Target)}";
            response.Code = AuthorizationResponseCode.Success;
            response.NotificationChannels = this.GetNotificationRecipients(matchedTarget.Notifications, true);
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
                return this.directory.TranslateName(s.ToString(), Interop.DsNameFormat.SecurityIdentifier, Interop.DsNameFormat.Nt4Name);
            }
            catch (Exception)
            {
                return sid;
            }
        }
    }
}