using Lithnet.Security.Authorization;

namespace Lithnet.AccessManager.Server.Authorization
{
    public interface IAuthorizationContextProvider
    {
        AuthorizationContext GetAuthorizationContext(IUser user, IComputer computer);
    }
}