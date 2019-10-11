using System.DirectoryServices.AccountManagement;
using System.Security.Claims;
using System.Web;
using Lithnet.Laps.Web.App_LocalResources;
using Lithnet.Laps.Web.Models;

namespace Lithnet.Laps.Web.Security.Authentication
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
                throw new NoMatchingPrincipalException(string.Format(LogMessages.UserNotFoundInDirectory, httpContext.User.Identity.Name));
            }

            IUser user = directory.GetUser(sid);

            if (user == null)
            {
                throw new NoMatchingPrincipalException(string.Format(LogMessages.UserNotFoundInDirectory, httpContext.User.Identity.Name));
            }

            return user;
        }
    }
}