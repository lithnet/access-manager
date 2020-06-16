namespace Lithnet.AccessManager.Web.AppSettings
{
    public interface IRateLimitSettings
    {
        IRateLimitThresholds PerIP { get; }

        IRateLimitThresholds PerUser { get; }

        IXffHandlerSettings XffHandling { get; }
    }
}