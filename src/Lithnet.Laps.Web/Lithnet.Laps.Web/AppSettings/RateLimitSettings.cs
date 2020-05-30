using Microsoft.Extensions.Configuration;

namespace Lithnet.Laps.Web
{
    public class RateLimitSettings : IRateLimitSettings
    {
        private IConfigurationRoot configuration;

        public RateLimitSettings(IConfigurationRoot configuration)
        {
            this.configuration = configuration;
            this.PerIP = new RateLimitDetails(this.configuration.GetSection("rate-limit:per-ip"));
            this.PerUser = new RateLimitDetails(this.configuration.GetSection("rate-limit:per-user"));
            this.XffHandling = new XffHandling(this.configuration);
        }

        public IRateLimitDetails PerIP { get; }

        public IRateLimitDetails PerUser { get; }

        public IXffHandling XffHandling { get; }
    }
}