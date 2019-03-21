using System.DirectoryServices.AccountManagement;
using System.Security.Claims;
using System.Web;
using Lithnet.Laps.Web.ActiveDirectory;
using Lithnet.Laps.Web.App_LocalResources;
using Lithnet.Laps.Web.Models;

namespace Lithnet.Laps.Web.Security.Authentication
{
    public class AuthenticationService: IAuthenticationService
    {
        public IUser GetLoggedInUser()
        {
            var httpContext = HttpContext.Current;

            if (httpContext?.User == null)
            {
                return (IUser) null;
            }

            // This was originally in LapController.GetCurrentUser().
            // I am clueless, as always :-)

            var principal = (ClaimsPrincipal)httpContext.User;
            string sid = principal.FindFirst(ClaimTypes.PrimarySid)?.Value;

            if (sid == null)
            {
                throw new NoMatchingPrincipalException(string.Format(LogMessages.UserNotFoundInDirectory, httpContext.User.Identity.Name));
            }
            var user = UserPrincipal.FindByIdentity(new PrincipalContext(ContextType.Domain), IdentityType.Sid, sid);

            return new UserAdapter(user);
        }
    }
}