using Microsoft.AspNetCore.Http;

namespace Lithnet.Laps.Web.Internal
{
    public interface IRateLimiter
    {
        RateLimitResult GetRateLimitResult(string userid, HttpRequest r);
    }
}
