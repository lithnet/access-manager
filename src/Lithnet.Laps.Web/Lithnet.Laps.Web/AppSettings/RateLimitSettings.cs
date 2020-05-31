using Microsoft.Extensions.Configuration;

namespace Lithnet.Laps.Web.AppSettings
{
    public class RateLimitSettings : IRateLimitSettings
    {
        private IConfigurationRoot configuration;

        public RateLimitSettings(IConfigurationRoot configuration)
        {
            this.configuration = configuration;
            this.PerIP = new RateLimitThresholds(this.configuration.GetSection("rate-limit:per-ip"));
            this.PerUser = new RateLimitThresholds(this.configuration.GetSection("rate-limit:per-user"));
            this.XffHandling = new XffHandlerSettings(this.configuration);
        }

        public IRateLimitThresholds PerIP { get; }

        public IRateLimitThresholds PerUser { get; }

        public IXffHandlerSettings XffHandling { get; }
    }
}