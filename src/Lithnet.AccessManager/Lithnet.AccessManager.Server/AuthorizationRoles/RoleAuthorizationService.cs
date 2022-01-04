using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Principal;
using System.Threading.Tasks;
using Lithnet.AccessManager.Interop;
using Lithnet.AccessManager.Server.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lithnet.AccessManager.Server.Authorization
{
    public class RoleAuthorizationService
    {
        private readonly IActiveDirectory directory;
        private readonly ILogger logger;
        private readonly RoleAuthorizationInformationBuilder authzBuilder;
        private readonly IRateLimiter rateLimiter;
        private readonly IOptionsSnapshot<AuthorizationOptions> authzOptions;

        public RoleAuthorizationService(IActiveDirectory directory, ILogger<SecurityDescriptorAuthorizationService> logger, RoleAuthorizationInformationBuilder authzBuilder, IRateLimiter rateLimiter, IOptionsSnapshot<AuthorizationOptions> authzOptions)
        {
            this.directory = directory;
            this.logger = logger;
            this.authzBuilder = authzBuilder;
            this.rateLimiter = rateLimiter;
            this.authzOptions = authzOptions;
        }

        public async Task<AuthorizationResponse> GetAuthorizationResponse(IActiveDirectoryUser user, string roleId, IPAddress ip)
        {
            try
            {
                var role = this.authzOptions.Value.Roles.SingleOrDefault(t => string.Equals(t.Id, roleId, StringComparison.OrdinalIgnoreCase));

                if (role == null || role.IsInactive())
                {
                    this.logger.LogTrace($"User {user.MsDsPrincipalName} is denied access to the role {roleId} because the role ID was not found");
                    return BuildAuthZResponseFailed(AuthorizationResponseCode.NoMatchingRuleForComputer);
                }

                var rateLimitResult = await this.rateLimiter.GetRateLimitResult(user.Sid, ip, AccessMask.Jit);

                if (rateLimitResult.IsRateLimitExceeded)
                {
                    return BuildAuthZResponseRateLimitExceeded(user, rateLimitResult, ip, role);
                }

                if (!this.authzBuilder.IsRoleAuthorized(user, role))
                {
                    return BuildAuthZResponseFailed(AuthorizationResponseCode.NoMatchingRuleForUser);
                }

                this.logger.LogTrace($"User {user.MsDsPrincipalName} is authorized for access to the role {role.RoleName}");
                return BuildAuthZResponseSuccess(role);
            }
            finally
            {
                this.authzBuilder.ClearCache(user);
            }
        }

        public async Task<IList<RoleSecurityDescriptorTarget>> GetPreAuthorization(IActiveDirectoryUser user)
        {
            var info = await this.authzBuilder.GetAuthorizationInformation(user);

            return info.MatchedTargets;
        }

        private static AuthorizationResponse BuildAuthZResponseFailed(AuthorizationResponseCode code, IList<string> failureNotificationRecipients = null)
        {
            AuthorizationResponse response = AuthorizationResponse.CreateAuthorizationResponse(AccessMask.Jit);
            response.Code = code;
            response.NotificationChannels = failureNotificationRecipients;
            return response;
        }

        private AuthorizationResponse BuildAuthZResponseRateLimitExceeded(IActiveDirectoryUser user, RateLimitResult result, IPAddress ip, RoleSecurityDescriptorTarget matchedTarget)
        {
            this.logger.LogError(result.IsUserRateLimit ? EventIDs.RateLimitExceededUser : EventIDs.RateLimitExceededIP, $"User {user.MsDsPrincipalName} on IP {ip} is denied access for role {matchedTarget.RoleName} because they have exceeded the {(result.IsUserRateLimit ? "user" : "IP")} rate limit of {result.Threshold}/{result.Duration.TotalSeconds} seconds");

            AuthorizationResponse response = AuthorizationResponse.CreateAuthorizationResponse(AccessMask.Jit);
            response.Code = result.IsUserRateLimit ? AuthorizationResponseCode.UserRateLimitExceeded : AuthorizationResponseCode.IpRateLimitExceeded;
            response.NotificationChannels = this.GetNotificationRecipients(matchedTarget.Notifications, false);

            return response;
        }

        private AuthorizationResponse BuildAuthZResponseSuccess(RoleSecurityDescriptorTarget matchedTarget)
        {
            AuthorizationResponse response;

            if (string.IsNullOrWhiteSpace(matchedTarget.Jit.AuthorizingGroup))
            {
                throw new ConfigurationException($"The target {matchedTarget.Id} has an empty JIT group");
            }

            response = new JitAuthorizationResponse()
            {
                ExpireAfter = matchedTarget.Jit.ExpireAfter,
                AuthorizingGroup = matchedTarget.Jit.AuthorizingGroup,
            };

            response.MatchedRule = matchedTarget.Id;
            response.MatchedRuleDescription = matchedTarget.RoleName;
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

        private IList<string> GetNotificationRecipients(IList<RoleSecurityDescriptorTarget> targets, bool success)
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