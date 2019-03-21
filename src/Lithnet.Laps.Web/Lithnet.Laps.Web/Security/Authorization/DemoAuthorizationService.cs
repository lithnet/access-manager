using Lithnet.Laps.Web.Audit;
using Lithnet.Laps.Web.Models;
using Lithnet.Laps.Web.Security.Authorization.ConfigurationFile;

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
            if (user.SamAccountName == "SomeUserName" && computer.SamAccountName.ToUpper() == "SomeComputerName")
            {
                return AuthorizationResponse.Authorized(new UsersToNotify(), "Demo authorization");
            }

            return AuthorizationResponse.Unauthorized();
        }
    }
}
