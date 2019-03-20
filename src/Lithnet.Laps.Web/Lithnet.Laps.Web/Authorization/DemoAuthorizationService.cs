using System.DirectoryServices.AccountManagement;
using Lithnet.Laps.Web.Audit;
using Lithnet.Laps.Web.Models;

namespace Lithnet.Laps.Web.Authorization
{
    public sealed class DemoAuthorizationService: IAuthorizationService
    {
        public AuthorizationResponse CanAccessPassword(UserPrincipal user, IComputer computer)
        {
            if (user?.SamAccountName == "u0115389" && computer.SamAccountName.ToUpper() == "GBW-L-W0499")
            {
                // authorized
                return AuthorizationResponse.Authorized(new UsersToNotify(), null);
            }

            // not authorized
            return AuthorizationResponse.Unauthorized(new UsersToNotify());
        }
    }
}
