using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Principal;
using System.Threading.Tasks;
using Lithnet.AccessManager.Interop;
using Lithnet.AccessManager.Server.Configuration;
using Microsoft.Extensions.Logging;

namespace Lithnet.AccessManager.Server.Authorization
{
    public class SecurityDescriptorAuthorizationService : IAuthorizationService
    {
        private readonly IActiveDirectory directory;
        private readonly ILogger logger;
        private readonly IJitAccessGroupResolver jitResolver;
        private readonly IAuthorizationInformationBuilder authzBuilder;
        private readonly IRateLimiter rateLimiter;

        public SecurityDescriptorAuthorizationService(IActiveDirectory directory, ILogger<SecurityDescriptorAuthorizationService> logger, IJitAccessGroupResolver jitResolver, IAuthorizationInformationBuilder authzBuilder, IRateLimiter rateLimiter)
        {
            this.directory = directory;
            this.logger = logger;
            this.jitResolver = jitResolver;
            this.authzBuilder = authzBuilder;
            this.rateLimiter = rateLimiter;
        }

        public async Task<AuthorizationResponse> GetAuthorizationResponse(IActiveDirectoryUser user, IComputer computer, AccessMask requestedAccess, IPAddress ip)
        {
            try
            {
                requestedAccess.ValidateAccessMask();

                var info = await this.authzBuilder.GetAuthorizationInformation(user, computer);

                if (info.MatchedComputerTargets.Count == 0)
                {
                    this.logger.LogTrace($"User {user.MsDsPrincipalName} is denied {requestedAccess} access to the computer {computer.FullyQualifiedName} because the computer did not match any of the configured targets");
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

                var matchedTarget = successTargets?.FirstOrDefault(t => t.IsActive());

                if (!info.EffectiveAccess.HasFlag(requestedAccess) || matchedTarget == null)
                {
                    this.logger.LogTrace($"User {user.MsDsPrincipalName} is denied {requestedAccess} access for computer {computer.FullyQualifiedName}");

                    return BuildAuthZResponseFailed(
                        requestedAccess,
                        AuthorizationResponseCode.NoMatchingRuleForUser,
                        GetNotificationRecipients(info.FailedTargets, false));
                }
                else
                {
                    var rateLimitResult = await this.rateLimiter.GetRateLimitResult(user.Sid, ip, requestedAccess);

                    if (rateLimitResult.IsRateLimitExceeded)
                    {
                        return BuildAuthZResponseRateLimitExceeded(user, computer, requestedAccess, rateLimitResult, ip, matchedTarget);
                    }

                    this.logger.LogTrace($"User {user.MsDsPrincipalName} is authorized for {requestedAccess} access to computer {computer.FullyQualifiedName} from target {matchedTarget.Id}");
                    return BuildAuthZResponseSuccess(requestedAccess, matchedTarget, computer);
                }
            }
            finally
            {
                this.authzBuilder.ClearCache(user, computer);
            }
        }

        public async Task<AuthorizationResponse> GetPreAuthorization(IActiveDirectoryUser user, IComputer computer)
        {
            var info = await this.authzBuilder.GetAuthorizationInformation(user, computer);

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

        private AuthorizationResponse BuildAuthZResponseRateLimitExceeded(IActiveDirectoryUser user, IComputer computer, AccessMask requestedAccess, RateLimitResult result, IPAddress ip, SecurityDescriptorTarget matchedTarget)
        {
            this.logger.LogError(result.IsUserRateLimit ? EventIDs.RateLimitExceededUser : EventIDs.RateLimitExceededIP , $"User {user.MsDsPrincipalName} on IP {ip} is denied {requestedAccess} access for computer {computer.FullyQualifiedName} because they have exceeded the {(result.IsUserRateLimit ? "user" : "IP")} rate limit of {result.Threshold}/{result.Duration.TotalSeconds} seconds");

            AuthorizationResponse response = AuthorizationResponse.CreateAuthorizationResponse(requestedAccess);
            response.Code = result.IsUserRateLimit ? AuthorizationResponseCode.UserRateLimitExceeded : AuthorizationResponseCode.IpRateLimitExceeded;
            response.NotificationChannels = this.GetNotificationRecipients(matchedTarget.Notifications, false);

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
                return this.directory.TranslateName(s.ToString(), DsNameFormat.SecurityIdentifier, DsNameFormat.Nt4Name);
            }
            catch (Exception)
            {
                return sid;
            }
        }
    }
}