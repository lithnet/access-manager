using System.Linq;
using System.Security.Claims;
using Lithnet.AccessManager.Server.Authorization;
using Lithnet.AccessManager.Service.App_LocalResources;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lithnet.AccessManager.Service.AppSettings
{
    public abstract class HttpContextAuthenticationProvider : IHttpContextAuthenticationProvider
    {
        private readonly IDirectory directory;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IAuthorizationContextProvider authzContextProvider;

        public abstract bool CanLogout { get; }

        public abstract bool IdpLogout { get; }

        protected HttpContextAuthenticationProvider(IHttpContextAccessor httpContextAccessor, IDirectory directory, IAuthorizationContextProvider authzContextProvider)
        {
            this.httpContextAccessor = httpContextAccessor;
            this.directory = directory;
            this.authzContextProvider = authzContextProvider;
        }

        protected void AddAuthZClaims(IUser user, ClaimsIdentity identity)
        {
            using var c = this.authzContextProvider.GetAuthorizationContext(user);

            identity.AddClaim(new Claim(ClaimTypes.GroupSid, user.Sid.ToString()));

            foreach (var g in c.GetTokenGroups())
            {
                identity.AddClaim(new Claim(ClaimTypes.GroupSid, g.ToString(), null, user.Sid.AccountDomainSid.ToString()));
            }
        }

        public IUser GetLoggedInUser()
        {
            string sid = this.GetLoggedInUserSid();

            return directory.GetUser(sid) ??
                throw new ObjectNotFoundException(string.Format(LogMessages.UserNotFoundInDirectory, this.httpContextAccessor.HttpContext.User?.Identity?.Name ?? "<unknown user>"));
        }

        private string GetLoggedInUserSid()
        {
            if (this.httpContextAccessor?.HttpContext?.User == null)
            {
                return null;
            }

            ClaimsPrincipal principal = this.httpContextAccessor.HttpContext.User;
            
            return principal.FindFirst(ClaimTypes.PrimarySid)?.Value ??
                throw new ObjectNotFoundException(string.Format(LogMessages.UserNotFoundInDirectory, this.httpContextAccessor.HttpContext.User?.Identity?.Name ?? "<unknown user>"));
        }

        public abstract void Configure(IServiceCollection services);
    }
}