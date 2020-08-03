using Lithnet.AccessManager.Server.Configuration;

namespace Lithnet.AccessManager.Server.Authorization
{
    public interface IAuthorizationService
    {
        AuthorizationResponse GetAuthorizationResponse(IUser user, IComputer computer, AccessMask requestedAccess);

        AuthorizationResponse GetPreAuthorization(IUser user, IComputer computer);
    }
}