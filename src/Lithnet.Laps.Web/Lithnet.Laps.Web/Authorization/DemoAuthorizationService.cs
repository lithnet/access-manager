using System.DirectoryServices.AccountManagement;
using Lithnet.Laps.Web.Audit;

namespace Lithnet.Laps.Web.Authorization
{
    public sealed class DemoAuthorizationService: IAuthorizationService
    {
        public AuthorizationResponse CanAccessPassword(UserPrincipal user, string computerName, TargetElement target = null)
        {
            if (user?.SamAccountName == "u0115389" && computerName.ToUpper() == "GBW-L-W0499")
            {
                // authorized
                return new AuthorizationResponse(true, new UsersToNotify(), "Demo authorization" );
            }

            // not authorized
            return new AuthorizationResponse(false, new UsersToNotify(), "Demo authorization" );
        }
    }
}
