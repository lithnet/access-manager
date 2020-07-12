using Lithnet.AccessManager.Server;

namespace Lithnet.AccessManager.Web.Authorization
{
    public interface IAuthorizationService
    {
        AuthorizationResponse GetAuthorizationResponse(IUser user, IComputer computer, AccessMask requestedAccess);
    }
}