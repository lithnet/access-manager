namespace Lithnet.Laps.Web
{
    public interface IRateLimitSettings
    {
        IRateLimitDetails PerIP { get; }

        IRateLimitDetails PerUser { get; }

        IXffHandling XffHandling { get; }
    }
}