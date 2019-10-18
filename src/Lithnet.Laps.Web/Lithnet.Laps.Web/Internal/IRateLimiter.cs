using System.Web;
using Lithnet.Laps.Web.Models;

namespace Lithnet.Laps.Web
{
    public interface IRateLimiter
    {
        bool IsRateLimitExceeded(LapRequestModel model, IUser p, HttpRequestBase r);
    }
}
