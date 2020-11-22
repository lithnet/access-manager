using System;
using System.Net;
using System.Security.Principal;
using System.Threading.Tasks;
using Lithnet.AccessManager.Server.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Lithnet.AccessManager.Server
{
    public sealed class MemoryCacheRateLimiter : IRateLimiter
    {
        private readonly RateLimitOptions rateLimits;
        private readonly IMemoryCache memoryCache;
        private readonly TimeSpan oneMinute = TimeSpan.FromMinutes(1);
        private readonly TimeSpan oneHour = TimeSpan.FromHours(1);
        private readonly TimeSpan oneDay = TimeSpan.FromDays(1);

        public MemoryCacheRateLimiter(IOptionsSnapshot<RateLimitOptions> rateLimits, IMemoryCache memoryCache)
        {
            this.rateLimits = rateLimits.Value;
            this.memoryCache = memoryCache;
        }

        public Task<RateLimitResult> GetRateLimitResult(SecurityIdentifier userid, IPAddress ip, AccessMask requestType)
        {
            if (this.rateLimits.PerIP.Enabled)
            {
                RateLimitResult result =
                    this.IsIpThresholdExceeded(ip, this.rateLimits.PerIP.RequestsPerMinute, oneMinute) ??
                    this.IsIpThresholdExceeded(ip, this.rateLimits.PerIP.RequestsPerHour, oneHour) ??
                    this.IsIpThresholdExceeded(ip, this.rateLimits.PerIP.RequestsPerDay, oneDay);

                if (result != null)
                {
                    result.UserID = userid.ToString();
                    result.IPAddress = ip;
                    return Task.FromResult(result);
                }
            }

            if (this.rateLimits.PerUser.Enabled)
            {
                RateLimitResult result =
                    this.IsUserThresholdExceeded(userid, this.rateLimits.PerUser.RequestsPerMinute, oneMinute) ??
                    this.IsUserThresholdExceeded(userid, this.rateLimits.PerUser.RequestsPerHour, oneHour) ??
                    this.IsUserThresholdExceeded(userid, this.rateLimits.PerUser.RequestsPerDay, oneDay);

                if (result != null)
                {
                    result.UserID = userid.ToString();
                    result.IPAddress = ip;
                    return Task.FromResult(result);
                }
            }

            return Task.FromResult(new RateLimitResult() { IsRateLimitExceeded = false });
        }

        private RateLimitResult IsIpThresholdExceeded(IPAddress ip, int threshold, TimeSpan duration)
        {
            if (this.IsThresholdExceeded(ip.ToString(), threshold, duration))
            {
                return new RateLimitResult { Duration = duration, IPAddress = ip, IsRateLimitExceeded = true, IsUserRateLimit = false, Threshold = threshold };
            }

            return null;
        }

        private RateLimitResult IsUserThresholdExceeded(SecurityIdentifier userid, int threshold, TimeSpan duration)
        {
            if (this.IsThresholdExceeded(userid.ToString(), threshold, duration))
            {
                return new RateLimitResult { Duration = duration, IsRateLimitExceeded = true, IsUserRateLimit = true, Threshold = threshold };
            }

            return null;
        }

        private bool IsThresholdExceeded(string usernameOrIP, int threshold, TimeSpan duration)
        {
            string key1 = string.Join(@"-", duration, threshold, usernameOrIP);
            return this.IsThresholdExceededForKey(key1, threshold, duration);
        }

        private bool IsThresholdExceededForKey(string key, int threshold, TimeSpan duration)
        {
            if (threshold <= 0)
            {
                return false;
            }

            if (!this.memoryCache.TryGetValue<int>(key, out int count))
            {
                count = 1;
            }
            else
            {
                count++;
            }

            this.memoryCache.Set<int>(
                key,
                count,
                DateTime.UtcNow.Add(duration)
            );

            return count > threshold;
        }
    }
}