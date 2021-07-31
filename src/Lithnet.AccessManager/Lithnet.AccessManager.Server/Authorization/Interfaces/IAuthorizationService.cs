using System.Net;
using System.Threading.Tasks;
using Lithnet.AccessManager.Server.Configuration;

namespace Lithnet.AccessManager.Server.Authorization
{
    public interface IAuthorizationService
    {
        Task<AuthorizationResponse> GetAuthorizationResponse(IActiveDirectoryUser user, IComputer computer, AccessMask requestedAccess, IPAddress ip);

        Task<AuthorizationResponse> GetPreAuthorization(IActiveDirectoryUser user, IComputer computer);
    }
}