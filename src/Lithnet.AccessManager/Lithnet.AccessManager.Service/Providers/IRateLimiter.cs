using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Lithnet.AccessManager.Service
{
    public interface IRateLimiter
    {
        Task<RateLimitResult> GetRateLimitResult(SecurityIdentifier userid, HttpRequest r);
    }
}
