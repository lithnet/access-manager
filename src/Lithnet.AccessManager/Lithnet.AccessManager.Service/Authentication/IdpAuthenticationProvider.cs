using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Lithnet.AccessManager.Server;
using Lithnet.AccessManager.Server.Auditing;
using Lithnet.AccessManager.Server.Authorization;
using Lithnet.AccessManager.Service.App_LocalResources;
using Lithnet.AccessManager.Service.Internal;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Lithnet.AccessManager.Service.AppSettings
{
    public abstract class IdpAuthenticationProvider : HttpContextAuthenticationProvider, IIdpAuthenticationProvider
    {
        private readonly ILogger logger;

        private readonly IActiveDirectory directory;

        protected abstract string ClaimName { get; }

        protected IdpAuthenticationProvider(ILogger logger, IActiveDirectory directory, IHttpContextAccessor httpContextAccessor, IAuthorizationContextProvider authzContextProvider)
            : base(httpContextAccessor, directory, authzContextProvider)
        {
            this.logger = logger;
            this.directory = directory;
        }

        public Task HandleAuthNFailed(AccessDeniedContext context)
        {
            this.logger.LogError(EventIDs.ExternalAuthNAccessDenied, context.Result?.Failure, LogMessages.AuthNAccessDenied);
            context.HandleResponse();
            context.Response.Redirect($"/Home/AuthNError?messageid={(int)AuthNFailureMessageID.ExternalAuthNProviderDenied}");

            return Task.CompletedTask;
        }

        public Task HandleRemoteFailure(RemoteFailureContext context)
        {
            this.logger.LogError(EventIDs.ExternalAuthNProviderError, context.Failure, LogMessages.AuthNProviderError);
            context.HandleResponse();
            context.Response.Redirect($"/Home/AuthNError?messageid={(int)AuthNFailureMessageID.ExternalAuthNProviderError}");

            return Task.CompletedTask;
        }

        public Task FindClaimIdentityInDirectoryOrFail<T>(RemoteAuthenticationContext<T> context) where T : AuthenticationSchemeOptions
        {
            try
            {
                ClaimsIdentity user = context.Principal.Identity as ClaimsIdentity;
                var directoryUser = this.FindUserByClaim(user, this.ClaimName);
                string sid = directoryUser?.Sid?.Value;

                if (sid == null)
                {
                    string message = string.Format(LogMessages.UserNotFoundInDirectory, user.ToClaimList());
                    this.logger.LogError(EventIDs.SsoIdentityNotFound, message);
                    context.HandleResponse();
                    context.Response.Redirect($"/Home/AuthNError?messageid={(int)AuthNFailureMessageID.SsoIdentityNotFound}");
                    return Task.CompletedTask;
                }
                
                user.AddClaim(new Claim(ClaimTypes.PrimarySid, sid));
                this.AddAuthZClaims(directoryUser, user);
                this.logger.LogInformation(EventIDs.UserAuthenticated, string.Format(LogMessages.AuthenticatedAndMappedUser, user.ToClaimList()));
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.AuthNResponseProcessingError, ex, LogMessages.AuthNResponseProcessingError);
                context.HandleResponse();
                context.Response.Redirect($"/Home/AuthNError?messageid={(int)AuthNFailureMessageID.SsoIdentityNotFound}");
                return Task.CompletedTask;
            }
        }

        private IActiveDirectoryUser FindUserByClaim(ClaimsIdentity p, string claimName)
        {
            Claim c = p.FindFirst(claimName);

            if (c != null)
            {
                this.logger.LogTrace($"Attempting to find a match in the directory for externally provided claim {c.Type}:{c.Value}");

                try
                {
                    return this.directory.GetUser(c.Value);
                }
                catch (Exception ex)
                {
                    this.logger.LogError(EventIDs.AuthNDirectoryLookupError, ex, string.Format(LogMessages.AuthNDirectoryLookupError, c.Type, c.Value));
                }
            }

            return null;
        }
    }
}