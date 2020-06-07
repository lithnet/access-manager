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
    public abstract class HttpContextAuthenticationProvider : IHttpContextAuthenticationProvider
    {
        private readonly IDirectory directory;

        private readonly IHttpContextAccessor httpContextAccessor;

        public abstract string ClaimName { get; }

        public abstract string UniqueClaimTypeIdentifier { get; }

        public abstract bool CanLogout { get; }

        public abstract bool IdpLogout { get; }

        public HttpContextAuthenticationProvider(IHttpContextAccessor httpContextAccessor, IDirectory directory)
        {
            this.httpContextAccessor = httpContextAccessor;
            this.directory = directory;
        }

        public IUser GetLoggedInUser()
        {
            string sid = this.GetLoggedInUserSid();

            return directory.GetUser(sid) ??
                throw new NotFoundException(string.Format(LogMessages.UserNotFoundInDirectory, this.httpContextAccessor.HttpContext.User?.Identity?.Name ?? "<unknown user>"));
        }

        private string GetLoggedInUserSid()
        {
            if (this.httpContextAccessor?.HttpContext?.User == null)
            {
                return null;
            }

            ClaimsPrincipal principal = this.httpContextAccessor.HttpContext.User;

            return principal.FindFirst(ClaimTypes.PrimarySid)?.Value ??
                throw new NotFoundException(string.Format(LogMessages.UserNotFoundInDirectory, this.httpContextAccessor.HttpContext.User?.Identity?.Name ?? "<unknown user>"));
        }
    }
}