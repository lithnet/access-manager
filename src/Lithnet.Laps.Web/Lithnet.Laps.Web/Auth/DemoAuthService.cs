using System;
using System.DirectoryServices.AccountManagement;

namespace Lithnet.Laps.Web.Auth
{
    public class DemoAuthService: IAuthService
    {
        public AuthResponse CanAccessPassword(UserPrincipal user, string computerName, TargetElement target = null)
        {
            if (user?.SamAccountName == "u0115389" && computerName.ToUpper() == "GBW-L-W0499")
            {
                // authorized
                return new AuthResponse(true, null);
            }

            // not authorized
            return new AuthResponse(false, null);
        }
    }
}
