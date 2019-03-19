using System;
using System.DirectoryServices.AccountManagement;
using System.Web;
using System.Web.Caching;
using Lithnet.Laps.Web.App_LocalResources;
using Lithnet.Laps.Web.Audit;
using Lithnet.Laps.Web.Models;

namespace Lithnet.Laps.Web
{
    internal static class RateLimiter
    {
        public static bool IsRateLimitExceeded(LapRequestModel model, UserPrincipal p, HttpRequestBase r)
        {
            if (LapsConfigSection.Configuration.RateLimitIP.Enabled)
            {
                if (RateLimiter.IsIpThresholdExceeded(model, p, r, LapsConfigSection.Configuration.RateLimitIP.ReqPerMinute, 60)
                    || RateLimiter.IsIpThresholdExceeded(model, p, r, LapsConfigSection.Configuration.RateLimitIP.ReqPerHour, 3600)
                    || RateLimiter.IsIpThresholdExceeded(model, p, r, LapsConfigSection.Configuration.RateLimitIP.ReqPerDay, 86400))
                {
                    return true;
                }
            }

            if (LapsConfigSection.Configuration.RateLimitUser.Enabled)
            {
                if (RateLimiter.IsUserThresholdExceeded(model, p, r, LapsConfigSection.Configuration.RateLimitUser.ReqPerMinute, 60)
                    || RateLimiter.IsUserThresholdExceeded(model, p, r, LapsConfigSection.Configuration.RateLimitUser.ReqPerHour, 3600)
                    || RateLimiter.IsUserThresholdExceeded(model, p, r, LapsConfigSection.Configuration.RateLimitUser.ReqPerDay, 86400))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsIpThresholdExceeded(LapRequestModel model, UserPrincipal p, HttpRequestBase r, int threshold, int duration)
        {
            if (RateLimiter.IsThresholdExceeded(r, threshold, duration))
            {
                Reporting.PerformAuditFailureActions(model, UIMessages.RateLimitError, EventIDs.RateLimitExceededIP,
                    string.Format(LogMessages.RateLimitExceededIP, p.SamAccountName, r.UserHostAddress, threshold, duration), null, null, null, p, null);
                return true;
            }

            return false;
        }

        private static bool IsUserThresholdExceeded(LapRequestModel model, UserPrincipal p, HttpRequestBase r, int threshold, int duration)
        {
            if (RateLimiter.IsThresholdExceeded(p, threshold, duration))
            {
                Reporting.PerformAuditFailureActions(model, UIMessages.RateLimitError, EventIDs.RateLimitExceededUser,
                    string.Format(LogMessages.RateLimitExceededUser, p.SamAccountName, r.UserHostAddress, threshold, duration), null, null, null, p, null);
                return true;
            }

            return false;
        }

        private static bool IsThresholdExceeded(HttpRequestBase r, int threshold, int duration)
        {
            string key1 = string.Join(@"-", duration, threshold, LapsConfigSection.Configuration.RateLimitIP.ThrottleOnXffIP ? r.GetUnmaskedIP() : r.UserHostAddress);

            return IsThresholdExceeded(key1, threshold, duration);
        }

        private static bool IsThresholdExceeded(UserPrincipal p, int threshold, int duration)
        {
            string key1 = string.Join(@"-", duration, threshold, p.Sid);

            return IsThresholdExceeded(key1, threshold, duration);
        }

        private static bool IsThresholdExceeded(string key, int threshold, int duration)
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