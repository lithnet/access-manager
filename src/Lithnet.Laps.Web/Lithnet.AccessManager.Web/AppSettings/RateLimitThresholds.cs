using Lithnet.AccessManager.Web.Internal;
using Microsoft.Extensions.Configuration;

namespace Lithnet.AccessManager.Web.AppSettings
{
    public class RateLimitThresholds : IRateLimitThresholds
    {
        private readonly IConfigurationSection configuration;

        public RateLimitThresholds(IConfigurationSection configuration)
        {
            this.configuration = configuration;
        }

        public bool Enabled => this.configuration.GetValueOrDefault("enabled", false);

        public int ReqPerMinute => this.configuration.GetValueOrDefault("requests-per-minute", 10);

        public int ReqPerHour => this.configuration.GetValueOrDefault("requests-per-hour", 50);

        public int ReqPerDay => this.configuration.GetValueOrDefault("requests-per-date", 100);
    }
}