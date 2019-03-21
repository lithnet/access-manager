using System;
using System.DirectoryServices.AccountManagement;
using System.Web;
using System.Web.Caching;
using Lithnet.Laps.Web.App_LocalResources;
using Lithnet.Laps.Web.Audit;
using Lithnet.Laps.Web.Models;

namespace Lithnet.Laps.Web
{
    public sealed class RateLimiter: IRateLimiter
    {
        private readonly LapsConfigSection configSection;
        private readonly Reporting reporting;

        public RateLimiter(LapsConfigSection configSection, Reporting reporting)
        {
            this.configSection = configSection;
            this.reporting = reporting;
        }

        public bool IsRateLimitExceeded(LapRequestModel model, IUser p, HttpRequestBase r)
        {
            if (configSection.Configuration.RateLimitIP.Enabled)
            {
                if (IsIpThresholdExceeded(model, p, r, configSection.Configuration.RateLimitIP.ReqPerMinute, 60)
                    || IsIpThresholdExceeded(model, p, r, configSection.Configuration.RateLimitIP.ReqPerHour, 3600)
                    || IsIpThresholdExceeded(model, p, r, configSection.Configuration.RateLimitIP.ReqPerDay, 86400))
                {
                    return true;
                }
            }

            if (configSection.Configuration.RateLimitUser.Enabled)
            {
                if (IsUserThresholdExceeded(model, p, r, configSection.Configuration.RateLimitUser.ReqPerMinute, 60)
                    || IsUserThresholdExceeded(model, p, r, configSection.Configuration.RateLimitUser.ReqPerHour, 3600)
                    || IsUserThresholdExceeded(model, p, r, configSection.Configuration.RateLimitUser.ReqPerDay, 86400))
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsIpThresholdExceeded(LapRequestModel model, IUser p, HttpRequestBase r, int threshold, int duration)
        {
            if (IsThresholdExceeded(r, threshold, duration))
            {
                reporting.PerformAuditFailureActions(model, UIMessages.RateLimitError, EventIDs.RateLimitExceededIP,
                    string.Format(LogMessages.RateLimitExceededIP, p.SamAccountName, r.UserHostAddress, threshold, duration), null, null, null, p, null);
                return true;
            }

            return false;
        }

        private bool IsUserThresholdExceeded(LapRequestModel model, IUser p, HttpRequestBase r, int threshold, int duration)
        {
            if (IsThresholdExceeded(p, threshold, duration))
            {
                reporting.PerformAuditFailureActions(model, UIMessages.RateLimitError, EventIDs.RateLimitExceededUser,
                    string.Format(LogMessages.RateLimitExceededUser, p.SamAccountName, r.UserHostAddress, threshold, duration), null, null, null, p, null);
                return true;
            }

            return false;
        }

        private bool IsThresholdExceeded(HttpRequestBase r, int threshold, int duration)
        {
            string key1 = string.Join(@"-", duration, threshold, configSection.Configuration.RateLimitIP.ThrottleOnXffIP ? r.GetUnmaskedIP() : r.UserHostAddress);

            return IsThresholdExceeded(key1, threshold, duration);
        }

        private bool IsThresholdExceeded(IUser p, int threshold, int duration)
        {
            string key1 = string.Join(@"-", duration, threshold, p.Sid);

            return IsThresholdExceeded(key1, threshold, duration);
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