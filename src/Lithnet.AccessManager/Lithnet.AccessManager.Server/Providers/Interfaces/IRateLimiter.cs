using System.Net;
using System.Security.Principal;
using System.Threading.Tasks;
using Lithnet.AccessManager.Server.Configuration;

namespace Lithnet.AccessManager.Server
{
    public interface IRateLimiter
    {
        Task<RateLimitResult> GetRateLimitResult(SecurityIdentifier userid, IPAddress r, AccessMask requestType);
    }
}
