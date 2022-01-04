using Lithnet.AccessManager.Server.App_LocalResources;
using Lithnet.AccessManager.Server.Auditing;
using Lithnet.AccessManager.Server.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Server.Authorization
{
    public class RoleFulfillmentService
    {
        private readonly IActiveDirectory directory;
        private readonly ILogger logger;
        private readonly IRateLimiter rateLimiter;
        private readonly RoleAuthorizationService authorizationService;
        private readonly IJitAccessProvider jitAccessProvider;
        private readonly IAuditEventProcessor reporting;

        public RoleFulfillmentService(IActiveDirectory directory, ILogger<SecurityDescriptorAuthorizationService> logger, IRateLimiter rateLimiter, RoleAuthorizationService authorizationService, IJitAccessProvider jitAccessProvider, IAuditEventProcessor reporting)
        {
            this.directory = directory;
            this.logger = logger;
            this.rateLimiter = rateLimiter;
            this.authorizationService = authorizationService;
            this.jitAccessProvider = jitAccessProvider;
            this.reporting = reporting;
        }

        public async Task<List<AvailableRole>> GetAvailableRoles(IActiveDirectoryUser user)
        {
            List<AvailableRole> availableRoles = new List<AvailableRole>();

            var roles = await this.authorizationService.GetPreAuthorization(user);

            foreach (RoleSecurityDescriptorTarget role in roles)
            {
                availableRoles.Add(new AvailableRole
                {
                    Key = role.Id,
                    Name = role.RoleName,
                    MaximumRequestDuration = role.Jit?.ExpireAfter ?? TimeSpan.FromMinutes(60),
                    ReasonRequired = role.ReasonRequired
                });
            }

            return availableRoles;
        }

        public async Task<RoleFulfillmentResult> RequestRole(IActiveDirectoryUser user, string roleId, IPAddress ipaddress, string requestReason, TimeSpan requestedDuration)
        {
            var authResponse = await this.authorizationService.GetAuthorizationResponse(user, roleId, ipaddress);

            if (!authResponse.IsAuthorized())
            {
                this.AuditAuthZFailure(requestReason, authResponse, user);
                return new RoleFulfillmentResult { IsSuccess = false, Error = RoleFulfillmentError.NotAuthorized };
            }

            return this.GrantJitAccess(user, (JitAuthorizationResponse)authResponse, requestReason, requestedDuration);
        }


        private void AuditAuthZFailure(string requestReason, AuthorizationResponse authorizationResponse, IActiveDirectoryUser user)
        {
            AuditableAction action = new AuditableAction
            {
                AuthzResponse = authorizationResponse,
                User = user,
                IsSuccess = false,
                RequestReason = requestReason,
                EvaluatedAccess = "JIT",
                Message = authorizationResponse.Code switch
                {
                    AuthorizationResponseCode.IpRateLimitExceeded => LogMessages.AuthZResponseIpRateLimitExceeded,
                    AuthorizationResponseCode.UserRateLimitExceeded => LogMessages.AuthZResponseUserRateLimitExceeded,
                    AuthorizationResponseCode.ExplicitlyDenied => LogMessages.AuthZResponseExplicitlyDenied,
                    AuthorizationResponseCode.NoMatchingRuleForComputer => LogMessages.AuthZResponseNoMatchingRuleForComputer,
                    AuthorizationResponseCode.NoMatchingRuleForUser => LogMessages.AuthZResponseNoMatchingRuleForUser,
                    _ => LogMessages.AuthZResponseFallback,
                },
                EventID = authorizationResponse.Code switch
                {
                    AuthorizationResponseCode.NoMatchingRuleForComputer => EventIDs.AuthZFailedNoTargetMatch,
                    AuthorizationResponseCode.NoMatchingRuleForUser => EventIDs.AuthZFailedNoReaderPrincipalMatch,
                    AuthorizationResponseCode.ExplicitlyDenied => EventIDs.AuthZExplicitlyDenied,
                    AuthorizationResponseCode.UserRateLimitExceeded => EventIDs.AuthZRateLimitExceeded,
                    AuthorizationResponseCode.IpRateLimitExceeded => EventIDs.AuthZRateLimitExceeded,
                    _ => EventIDs.AuthZFailed,
                }
            };

            this.reporting.GenerateAuditEvent(action);
        }

        private RoleFulfillmentResult GrantJitAccess(IActiveDirectoryUser user, JitAuthorizationResponse authResponse, string requestReason, TimeSpan requestedDuration)
        {
            Action undo = null;

            try
            {
                TimeSpan expiryAfter = TimeSpan.FromTicks(Math.Min(authResponse.ExpireAfter.Ticks, requestedDuration.Ticks));

                TimeSpan grantedAccessLength = this.jitAccessProvider.GrantJitAccess(this.directory.GetGroup(authResponse.AuthorizingGroup), user, authResponse.AllowExtension, expiryAfter, out undo);

                DateTime expiryDate = DateTime.Now.Add(grantedAccessLength);

                this.reporting.GenerateAuditEvent(new AuditableAction
                {
                    AuthzResponse = authResponse,
                    RequestReason = requestReason,
                    EvaluatedAccess = "JIT",
                    IsSuccess = true,
                    User = user,
                    EventID = EventIDs.ComputerJitAccessGranted,
                    AccessExpiryDate = expiryDate.ToString(CultureInfo.CurrentCulture)
                });

                return new RoleFulfillmentResult
                {
                    Error = 0,
                    IsSuccess = true,
                    Expiry = expiryDate,
                    RoleName = authResponse.MatchedRuleDescription
                };
            }
            catch (Exception ex)
            {
                if (undo != null)
                {
                    this.logger.LogWarning(EventIDs.JitRollbackInProgress, ex, LogMessages.JitRollbackInProgress, user.MsDsPrincipalName, authResponse.MatchedRule);

                    try
                    {
                        undo();
                    }
                    catch (Exception ex2)
                    {
                        this.logger.LogError(EventIDs.JitRollbackFailed, ex2, LogMessages.JitRollbackFailed, user.MsDsPrincipalName, authResponse.MatchedRule);
                    }
                }

                this.logger.LogError(EventIDs.JitError, ex, string.Format(LogMessages.JitError, authResponse.MatchedRule, user.MsDsPrincipalName));

                return new RoleFulfillmentResult
                {
                    IsSuccess = false,
                    Error = RoleFulfillmentError.FulfillmentError
                };
            }
        }
    }
}