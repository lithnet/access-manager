namespace Lithnet.AccessManager.Web.AppSettings
{
    public interface IRateLimitThresholds
    {
        bool Enabled { get; }

        int ReqPerMinute { get; }

        int ReqPerHour { get; }

        int ReqPerDay { get; }
    }
}