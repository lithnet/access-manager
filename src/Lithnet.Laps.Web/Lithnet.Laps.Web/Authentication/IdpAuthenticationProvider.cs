using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Lithnet.Laps.Web.ActiveDirectory;
using Lithnet.Laps.Web.App_LocalResources;
using Lithnet.Laps.Web.Internal;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using NLog;

namespace Lithnet.Laps.Web.AppSettings
{
    public abstract class IdpAuthenticationProvider : HttpContextAuthenticationProvider, IIdpAuthenticationProvider
    {
        private readonly ILogger logger;

        private readonly IDirectory directory;

        public IdpAuthenticationProvider( ILogger logger, IDirectory directory, IHttpContextAccessor httpContextAccessor)
            :base(httpContextAccessor, directory)
        {
            this.logger = logger;
            this.directory = directory;
        }

        public Task HandleAuthNFailed(AccessDeniedContext context)
        {
            this.logger.LogEventError(EventIDs.ExternalAuthNAccessDenied, LogMessages.AuthNAccessDenied, context.Result?.Failure);
            context.HandleResponse();
            context.Response.Redirect($"/Home/AuthNError?messageid={(int)AuthNFailureMessageID.ExternalAuthNProviderDenied}");

            return Task.CompletedTask;
        }

        public Task HandleRemoteFailure(RemoteFailureContext context)
        {
            this.logger.LogEventError(EventIDs.ExternalAuthNProviderError, LogMessages.AuthNProviderError, context.Failure);
            context.HandleResponse();
            context.Response.Redirect($"/Home/AuthNError?messageid={(int)AuthNFailureMessageID.ExternalAuthNProviderError}");

            return Task.CompletedTask;
        }

        public Task FindClaimIdentityInDirectoryOrFail<T>(RemoteAuthenticationContext<T> context) where T : AuthenticationSchemeOptions
        {
            try
            {
                ClaimsIdentity user = context.Principal.Identity as ClaimsIdentity;
                string sid = this.FindUserByClaim(user, this.ClaimName)?.Sid?.Value;

                if (sid == null)
                {
                    string message = string.Format(LogMessages.UserNotFoundInDirectory, user.ToClaimList());
                    this.logger.LogEventError(EventIDs.SsoIdentityNotFound, message, null);
                    context.HandleResponse();
                    context.Response.Redirect($"/Home/AuthNError?messageid={(int)AuthNFailureMessageID.SsoIdentityNotFound}");
                    return Task.CompletedTask;
                }

                user.AddClaim(new Claim(ClaimTypes.PrimarySid, sid));
                this.logger.LogEventSuccess(EventIDs.UserAuthenticated, string.Format(LogMessages.AuthenticatedAndMappedUser, user.ToClaimList()));
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                this.logger.LogEventError(EventIDs.AuthNResponseProcessingError, LogMessages.AuthNResponseProcessingError, ex);
                context.HandleResponse();
                context.Response.Redirect($"/Home/AuthNError?messageid={(int)AuthNFailureMessageID.SsoIdentityNotFound}");
                return Task.CompletedTask;
            }
        }

        private IUser FindUserByClaim(ClaimsIdentity p, string claimName)
        {
            Claim c = p.FindFirst(claimName);

            if (c != null)
            {
                this.logger.Trace($"Attempting to find a match in the directory for externally provided claim {c.Type}:{c.Value}");

                try
                {
                    return this.directory.GetUser(c.Value);
                }
                catch (Exception ex)
                {
                    this.logger.LogEventError(EventIDs.AuthNDirectoryLookupError, string.Format(LogMessages.AuthNDirectoryLookupError, c.Type, c.Value), ex);
                }
            }

            return null;
        }
    }
}