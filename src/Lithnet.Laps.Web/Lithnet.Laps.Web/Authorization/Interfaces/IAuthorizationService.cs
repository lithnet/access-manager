using Lithnet.Laps.Web.ActiveDirectory;

namespace Lithnet.Laps.Web.Authorization
{
    public interface IAuthorizationService
    {
        AuthorizationResponse GetAuthorizationResponse(IUser user, IComputer computer);
    }
}