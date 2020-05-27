using System;
using System.Web;
using System.Web.Caching;
using Lithnet.Laps.Web.App_LocalResources;
using Lithnet.Laps.Web.Audit;
using Lithnet.Laps.Web.Config;
using Lithnet.Laps.Web.Models;

namespace Lithnet.Laps.Web
{
    public sealed class RateLimiter: IRateLimiter
    {
        private readonly ILapsConfig configSection;
        private readonly Reporting reporting;

        public RateLimiter(ILapsConfig configSection, Reporting reporting)
        {
            this.configSection = configSection;
            this.reporting = reporting;
        }

        public bool IsRateLimitExceeded(LapRequestModel model, IUser p, HttpRequestBase r)
        {
            if (this.configSection.RateLimitIP.Enabled)
            {
                if (this.IsIpThresholdExceeded(model, p, r, this.configSection.RateLimitIP.ReqPerMinute, 60)
                    || this.IsIpThresholdExceeded(model, p, r, this.configSection.RateLimitIP.ReqPerHour, 3600)
                    || this.IsIpThresholdExceeded(model, p, r, this.configSection.RateLimitIP.ReqPerDay, 86400))
                {
                    return true;
                }
            }

            if (this.configSection.RateLimitUser.Enabled)
            {
                if (this.IsUserThresholdExceeded(model, p, r, this.configSection.RateLimitUser.ReqPerMinute, 60)
                    || this.IsUserThresholdExceeded(model, p, r, this.configSection.RateLimitUser.ReqPerHour, 3600)
                    || this.IsUserThresholdExceeded(model, p, r, this.configSection.RateLimitUser.ReqPerDay, 86400))
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsIpThresholdExceeded(LapRequestModel model, IUser p, HttpRequestBase r, int threshold, int duration)
        {
            if (this.IsThresholdExceeded(r, threshold, duration))
            {
                this.reporting.PerformAuditFailureActions(model, UIMessages.RateLimitError, EventIDs.RateLimitExceededIP,
                    string.Format(LogMessages.RateLimitExceededIP, p.SamAccountName, r.UserHostAddress, threshold, duration), null, null, null, p, null);
                return true;
            }

            return false;
        }

        private bool IsUserThresholdExceeded(LapRequestModel model, IUser p, HttpRequestBase r, int threshold, int duration)
        {
            if (this.IsThresholdExceeded(p, threshold, duration))
            {
                this.reporting.PerformAuditFailureActions(model, UIMessages.RateLimitError, EventIDs.RateLimitExceededUser,
                    string.Format(LogMessages.RateLimitExceededUser, p.SamAccountName, r.UserHostAddress, threshold, duration), null, null, null, p, null);
                return true;
            }

            return false;
        }

        private bool IsThresholdExceeded(HttpRequestBase r, int threshold, int duration)
        {
            string key1 = string.Join(@"-", duration, threshold, this.configSection.RateLimitIP.ThrottleOnXffIP ? r.GetUnmaskedIP() : r.UserHostAddress);

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