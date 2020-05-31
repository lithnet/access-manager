using System.Security.Claims;
using System.Web;
using Lithnet.Laps.Web.App_LocalResources;
using Lithnet.Laps.Web.ActiveDirectory;

namespace Lithnet.Laps.Web.Internal
{
    public class AuthenticationService : IAuthenticationService
    {
        public IUser GetLoggedInUser(IDirectory directory)
        {
            HttpContext httpContext = HttpContext.Current;

            if (httpContext?.User == null)
            {
                return null;
            }

            ClaimsPrincipal principal = (ClaimsPrincipal)httpContext.User;
            string sid = principal.FindFirst(ClaimTypes.PrimarySid)?.Value;

            if (sid == null)
            {
                throw new NotFoundException(string.Format(LogMessages.UserNotFoundInDirectory, httpContext.User.Identity.Name));
            }

            IUser user = directory.GetUser(sid);

            if (user == null)
            {
                throw new NotFoundException(string.Format(LogMessages.UserNotFoundInDirectory, httpContext.User.Identity.Name));
            }

            return user;
        }
    }
}