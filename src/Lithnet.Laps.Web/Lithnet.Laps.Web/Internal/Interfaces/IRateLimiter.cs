using System.Web;

namespace Lithnet.Laps.Web.Internal
{
    public interface IRateLimiter
    {
        RateLimitResult GetRateLimitResult(string userid, HttpRequestBase r);
    }
}
