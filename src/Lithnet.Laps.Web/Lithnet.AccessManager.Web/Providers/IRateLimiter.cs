using Microsoft.AspNetCore.Http;

namespace Lithnet.AccessManager.Web.Internal
{
    public interface IRateLimiter
    {
        RateLimitResult GetRateLimitResult(string userid, HttpRequest r);
    }
}
