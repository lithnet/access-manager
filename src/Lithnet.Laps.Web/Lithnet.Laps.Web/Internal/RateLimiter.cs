using System;
using System.Web;
using System.Web.Caching;
using Lithnet.Laps.Web.AppSettings;

namespace Lithnet.Laps.Web.Internal
{
    public sealed class RateLimiter : IRateLimiter
    {
        private readonly IRateLimitSettings rateLimits;
        private readonly IIpAddressResolver ipResolver;

        public RateLimiter(IRateLimitSettings rateLimits, IIpAddressResolver ipResolver)
        {
            this.rateLimits = rateLimits;
            this.ipResolver = ipResolver;
        }

        public RateLimitResult GetRateLimitResult(string userid, HttpRequestBase r)
        {
            if (this.rateLimits.PerIP.Enabled)
            {
                RateLimitResult result =
                    this.IsIpThresholdExceeded(r, this.rateLimits.PerIP.ReqPerMinute, 60) ??
                    this.IsIpThresholdExceeded(r, this.rateLimits.PerIP.ReqPerHour, 3600) ??
                    this.IsIpThresholdExceeded(r, this.rateLimits.PerIP.ReqPerDay, 86400);

                if (result != null)
                {
                    result.UserID = userid;
                    result.IPAddress = this.ipResolver.GetRequestIP(r);
                    return result;
                }
            }

            if (this.rateLimits.PerUser.Enabled)
            {
                RateLimitResult result =
                    this.IsUserThresholdExceeded(userid, this.rateLimits.PerUser.ReqPerMinute, 60) ??
                    this.IsUserThresholdExceeded(userid, this.rateLimits.PerUser.ReqPerHour, 3600) ??
                    this.IsUserThresholdExceeded(userid, this.rateLimits.PerUser.ReqPerDay, 86400);

                if (result != null)
                {
                    result.UserID = userid;
                    result.IPAddress = this.ipResolver.GetRequestIP(r);
                    return result;
                }
            }

            return new RateLimitResult() { IsRateLimitExceeded = false };
        }

        private RateLimitResult IsIpThresholdExceeded(HttpRequestBase r, int threshold, int duration)
        {
            string ip = this.ipResolver.GetRequestIP(r);

            if (this.IsThresholdExceededIP(ip, threshold, duration))
            {
                return new RateLimitResult { Duration = duration, IPAddress = ip, IsRateLimitExceeded = true, IsUserRateLimit = false, Threshold = threshold };
            }

            return null;
        }

        private RateLimitResult IsUserThresholdExceeded(string userid, int threshold, int duration)
        {
            if (this.IsThresholdExceededUserID(userid, threshold, duration))
            {
                return new RateLimitResult { Duration = duration, IsRateLimitExceeded = true, IsUserRateLimit = true, Threshold = threshold };
            }

            return null;
        }

        private bool IsThresholdExceededIP(string ip, int threshold, int duration)
        {
            string key1 = string.Join(@"-", duration, threshold, ip);
            return this.IsThresholdExceeded(key1, threshold, duration);
        }

        private bool IsThresholdExceededUserID(string userid, int threshold, int duration)
        {
            string key1 = string.Join(@"-", duration, threshold, userid);

            return this.IsThresholdExceeded(key1, threshold, duration);
        }

        private bool IsThresholdExceeded(string key, int threshold, int duration)
        {
            int count = 1;

            if (HttpRuntime.Cache[key] != null)
            {
                count = (int)HttpRuntime.Cache[key] + 1;
            }

            HttpRuntime.Cache.Insert(
                key,
                count,
                null,
                DateTime.UtcNow.AddSeconds(duration),
                Cache.NoSlidingExpiration,
                CacheItemPriority.Low,
                null
            );

            return count > threshold;
        }
    }
}