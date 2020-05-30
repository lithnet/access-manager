using Lithnet.Laps.Web.Models;

namespace Lithnet.Laps.Web.JsonTargets
{
    public interface IAuthorizationService
    {
        AuthorizationResponse GetAuthorizationResponse(IUser user, IComputer computer);
    }
}