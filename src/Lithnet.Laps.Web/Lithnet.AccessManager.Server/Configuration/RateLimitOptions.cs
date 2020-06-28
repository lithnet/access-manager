namespace Lithnet.AccessManager.Configuration
{
    public class RateLimitOptions
    {
        public RateLimitThresholds PerIP { get; set; }

        public RateLimitThresholds PerUser { get; set; }
    }
}