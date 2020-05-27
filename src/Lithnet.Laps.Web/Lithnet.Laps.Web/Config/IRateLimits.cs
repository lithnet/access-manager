namespace Lithnet.Laps.Web
{
    public interface IRateLimits
    {
        RateLimitDetails PerIP { get; set; }

        RateLimitDetails PerUser { get; set; }
    }
}