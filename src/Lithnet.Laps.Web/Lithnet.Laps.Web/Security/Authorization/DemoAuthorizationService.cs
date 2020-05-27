using Lithnet.Laps.Web.Audit;
using Lithnet.Laps.Web.Config;
using Lithnet.Laps.Web.Models;

namespace Lithnet.Laps.Web.Security.Authorization
{
    /// <summary>
    /// Demo authorization service.
    ///
    /// This is only to show how you can implement a custom authorization service.
    /// </summary>
    public sealed class DemoAuthorizationService: IAuthorizationService
    {
        public AuthorizationResponse CanAccessPassword(IUser user, IComputer computer, ITarget target)
        {
            if (user.SamAccountName == "SomeUserName" && string.Equals(computer.SamAccountName, "SomeComputerName", System.StringComparison.OrdinalIgnoreCase))
            {
                return AuthorizationResponse.Authorized(new UsersToNotify(), "Demo authorization");
            }

            return AuthorizationResponse.Unauthorized();
        }
    }
}
