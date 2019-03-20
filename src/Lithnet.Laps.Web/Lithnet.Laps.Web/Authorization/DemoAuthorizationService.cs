using Lithnet.Laps.Web.Audit;
using Lithnet.Laps.Web.Models;

namespace Lithnet.Laps.Web.Authorization
{
    public sealed class DemoAuthorizationService: IAuthorizationService
    {
        public AuthorizationResponse CanAccessPassword(string userName, IComputer computer)
        {
            if (userName == "u0115389" && computer.SamAccountName.ToUpper() == "GBW-L-W0499")
            {
                return AuthorizationResponse.Authorized(new UsersToNotify(), null);
            }

            return AuthorizationResponse.Unauthorized(new UsersToNotify());
        }
    }
}
