using System.Security.Principal;
using Lithnet.Security.Authorization;

namespace Lithnet.AccessManager.Server.Authorization
{
    public interface IAuthorizationContextProvider
    {
        AuthorizationContext GetAuthorizationContext(IActiveDirectoryUser user, SecurityIdentifier resourceDomainSid);

        AuthorizationContext GetAuthorizationContext(IActiveDirectoryUser user);
    }
}