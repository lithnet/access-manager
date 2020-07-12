namespace Lithnet.AccessManager.Configuration
{
    public class RateLimitOptions
    {
        public RateLimitThresholds PerIP { get; set; } = new RateLimitThresholds();

        public RateLimitThresholds PerUser { get; set; } = new RateLimitThresholds();
    }
}