using System.Security.Principal;
using Lithnet.Security.Authorization;

namespace Lithnet.AccessManager.Server.Authorization
{
    public interface IAuthorizationContextProvider
    {
        AuthorizationContext GetAuthorizationContext(IUser user, SecurityIdentifier resourceDomainSid);

        AuthorizationContext GetAuthorizationContext(IUser user);
    }
}