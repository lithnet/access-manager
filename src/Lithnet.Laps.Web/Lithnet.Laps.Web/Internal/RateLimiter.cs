using System;
using System.Web;
using System.Web.Caching;
using Lithnet.Laps.Web.App_LocalResources;
using Lithnet.Laps.Web.Audit;
using Lithnet.Laps.Web.Internal;
using Lithnet.Laps.Web.Models;

namespace Lithnet.Laps.Web
{
    public sealed class RateLimiter: IRateLimiter
    {
        private readonly IRateLimitSettings rateLimits;
        private readonly IIpAddressResolver ipResolver;
        private readonly IReporting reporting;

        public RateLimiter(IRateLimitSettings rateLimits, IIpAddressResolver ipResolver, IReporting reporting)
        {
            this.rateLimits = rateLimits;
            this.reporting = reporting;
            this.ipResolver = ipResolver;
        }

        public bool IsRateLimitExceeded(LapRequestModel model, IUser p, HttpRequestBase r)
        {
            if (this.rateLimits.PerIP.Enabled)
            {
                if (this.IsIpThresholdExceeded(model, p, r, this.rateLimits.PerIP.ReqPerMinute, 60)
                    || this.IsIpThresholdExceeded(model, p, r, this.rateLimits.PerIP.ReqPerHour, 3600)
                    || this.IsIpThresholdExceeded(model, p, r, this.rateLimits.PerIP.ReqPerDay, 86400))
                {
                    return true;
                }
            }

            if (this.rateLimits.PerUser.Enabled)
            {
                if (this.IsUserThresholdExceeded(model, p, r, this.rateLimits.PerUser.ReqPerMinute, 60)
                    || this.IsUserThresholdExceeded(model, p, r, this.rateLimits.PerUser.ReqPerHour, 3600)
                    || this.IsUserThresholdExceeded(model, p, r, this.rateLimits.PerUser.ReqPerDay, 86400))
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsIpThresholdExceeded(LapRequestModel model, IUser p, HttpRequestBase r, int threshold, int duration)
        {
            string ip = this.ipResolver.GetRequestIP(r);

            if (this.IsThresholdExceeded(r, ip, threshold, duration))
            {
                this.reporting.PerformAuditFailureActions(model, UIMessages.RateLimitError, EventIDs.RateLimitExceededIP,
                    string.Format(LogMessages.RateLimitExceededIP, p.SamAccountName, ip, threshold, duration), null, null, p, null);
                return true;
            }

            return false;
        }

        private bool IsUserThresholdExceeded(LapRequestModel model, IUser p, HttpRequestBase r, int threshold, int duration)
        {
            string ip = this.ipResolver.GetRequestIP(r);

            if (this.IsThresholdExceeded(p, threshold, duration))
            {
                this.reporting.PerformAuditFailureActions(model, UIMessages.RateLimitError, EventIDs.RateLimitExceededUser,
                    string.Format(LogMessages.RateLimitExceededUser, p.SamAccountName, ip, threshold, duration), null, null, p, null);
                return true;
            }

            return false;
        }

        private bool IsThresholdExceeded(HttpRequestBase r, string ip, int threshold, int duration)
        {
            string key1 = string.Join(@"-", duration, threshold, ip);
            return this.IsThresholdExceeded(key1, threshold, duration);
        }

        private bool IsThresholdExceeded(IUser p, int threshold, int duration)
        {
            string key1 = string.Join(@"-", duration, threshold, p.Sid);

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