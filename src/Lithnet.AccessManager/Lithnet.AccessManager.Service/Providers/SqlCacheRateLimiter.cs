using System;
using System.Data.Common;
using System.Data.SqlClient;
using System.Net;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Lithnet.AccessManager.Server;
using Lithnet.AccessManager.Server.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Lithnet.AccessManager.Service
{
    public sealed class SqlCacheRateLimiter : IRateLimiter
    {
        private readonly RateLimitOptions rateLimits;
        private readonly IDbProvider dbProvider;
        private readonly TimeSpan oneMinute = TimeSpan.FromMinutes(1);
        private readonly TimeSpan oneHour = TimeSpan.FromHours(1);
        private readonly TimeSpan oneDay = TimeSpan.FromDays(1);
        private readonly TimeSpan maxCacheAge = TimeSpan.FromDays(1);
        private readonly TimeSpan cleanupInterval = TimeSpan.FromHours(1);

        private int runningCleanup;
        private DateTime? lastCleanup;

        public SqlCacheRateLimiter(IOptionsSnapshot<RateLimitOptions> rateLimits, IDbProvider dbProvider)
        {
            this.rateLimits = rateLimits.Value;
            this.dbProvider = dbProvider;
        }

        public async Task<RateLimitResult> GetRateLimitResult(SecurityIdentifier userid, HttpRequest r)
        {
            if (this.rateLimits.PerIP.Enabled)
            {
                RateLimitResult result =
                    await this.IsIpThresholdExceeded(r, this.rateLimits.PerIP.RequestsPerMinute, oneMinute) ??
                    await this.IsIpThresholdExceeded(r, this.rateLimits.PerIP.RequestsPerHour, oneHour) ??
                    await this.IsIpThresholdExceeded(r, this.rateLimits.PerIP.RequestsPerDay, oneDay);

                if (result != null)
                {
                    result.UserID = userid.ToString();
                    result.IPAddress = r.HttpContext.Connection.RemoteIpAddress.ToString();
                    return result;
                }

                await this.AddEntryAsync(r.HttpContext.Connection.RemoteIpAddress.ToString());
            }

            if (this.rateLimits.PerUser.Enabled)
            {
                RateLimitResult result =
                    await this.IsUserThresholdExceeded(userid, this.rateLimits.PerUser.RequestsPerMinute, oneMinute) ??
                    await this.IsUserThresholdExceeded(userid, this.rateLimits.PerUser.RequestsPerHour, oneHour) ??
                    await this.IsUserThresholdExceeded(userid, this.rateLimits.PerUser.RequestsPerDay, oneDay);

                if (result != null)
                {
                    result.UserID = userid.ToString();
                    result.IPAddress = r.HttpContext.Connection.RemoteIpAddress.ToString();
                    return result;
                }

                await this.AddEntryAsync(userid.ToString());
            }

            return new RateLimitResult() { IsRateLimitExceeded = false };
        }

        private async Task<RateLimitResult> IsIpThresholdExceeded(HttpRequest r, int threshold, TimeSpan duration)
        {
            var ip = r.HttpContext.Connection.RemoteIpAddress;

            if (await this.HasRateLimitExceededAsync(ip.ToString(), duration, threshold))
            {
                return new RateLimitResult { Duration = duration, IPAddress = ip.ToString(), IsRateLimitExceeded = true, IsUserRateLimit = false, Threshold = threshold };
            }


            return null;
        }

        private async Task<RateLimitResult> IsUserThresholdExceeded(SecurityIdentifier userid, int threshold, TimeSpan duration)
        {
            if (await this.HasRateLimitExceededAsync(userid.ToString(), duration, threshold))
            {
                return new RateLimitResult { Duration = duration, IsRateLimitExceeded = true, IsUserRateLimit = true, Threshold = threshold };
            }

            return null;
        }

        public async Task AddEntryAsync(string key)
        {
            using (var con = this.dbProvider.GetConnection())
            {
                string sql = "INSERT INTO [AccessManager].[dbo].[RateLimitCache] (CacheKey, Created) VALUES (@key, @created)";
                SqlCommand command = new SqlCommand(sql, con);
                command.Parameters.AddWithValue("@key", key);
                command.Parameters.AddWithValue("@created", DateTime.UtcNow);
                await command.ExecuteNonQueryAsync();
            }

            await this.CleanUpIfRequiredAsync();
        }

        public async Task<bool> HasRateLimitExceededAsync(string key, TimeSpan interval, int limit)
        {
            string sql = "SELECT COUNT(*) FROM [AccessManager].[dbo].[RateLimitCache] WHERE CacheKey = @key and created > @created";
            int? value;

            using (var con = this.dbProvider.GetConnection())
            {
                SqlCommand command = new SqlCommand(sql, con);
                command.Parameters.AddWithValue("@key", key);
                command.Parameters.AddWithValue("@created", DateTime.UtcNow.Subtract(interval));

                value = await command.ExecuteScalarAsync() as int?;
            }

            await this.CleanUpIfRequiredAsync();

            return value != null && value >= limit;
        }

        public async Task CleanUpIfRequiredAsync()
        {
            if (Interlocked.CompareExchange(ref runningCleanup, 1, 0) == 1)
            {
                return;
            }

            try
            {
                if (lastCleanup != null && lastCleanup.Value.Add(this.cleanupInterval) > DateTime.UtcNow)
                {
                    return;
                }

                this.lastCleanup = DateTime.UtcNow;

                using (var con = this.dbProvider.GetConnection())
                {
                    string sql = "DELETE FROM [AccessManager].[dbo].[RateLimitCache] WHERE Created < @created";

                    SqlCommand command = new SqlCommand(sql, con);
                    command.Parameters.AddWithValue("@created", DateTime.UtcNow.Subtract(maxCacheAge));
                    await command.ExecuteNonQueryAsync();
                }
            }
            finally
            {
                runningCleanup = 0;
            }
        }
    }
}