using Microsoft.AspNetCore.Http;

namespace Lithnet.AccessManager.Service
{
    public interface IRateLimiter
    {
        RateLimitResult GetRateLimitResult(string userid, HttpRequest r);
    }
}
