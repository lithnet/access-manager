namespace Lithnet.AccessManager.Server.Configuration
{
    public class RateLimitOptions
    {
        public RateLimitThresholds PerIP { get; set; } = new RateLimitThresholds();

        public RateLimitThresholds PerUser { get; set; } = new RateLimitThresholds();
    }
}