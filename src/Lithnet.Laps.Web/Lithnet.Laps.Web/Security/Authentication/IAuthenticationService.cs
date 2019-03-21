using Lithnet.Laps.Web.Models;

namespace Lithnet.Laps.Web.Security.Authentication
{
    public interface IAuthenticationService
    {
        IUser GetLoggedInUser();
    }
}
