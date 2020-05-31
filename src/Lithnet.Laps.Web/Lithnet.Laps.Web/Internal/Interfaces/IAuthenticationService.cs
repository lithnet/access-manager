using Lithnet.Laps.Web.ActiveDirectory;

namespace Lithnet.Laps.Web.Internal
{
    public interface IAuthenticationService
    {
        IUser GetLoggedInUser(IDirectory directory);
    }
}
