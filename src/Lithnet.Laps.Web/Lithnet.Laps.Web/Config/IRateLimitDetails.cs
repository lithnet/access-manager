namespace Lithnet.Laps.Web
{
    public interface IRateLimitDetails
    {
        bool Enabled { get; }

        int ReqPerMinute { get; }

        int ReqPerHour { get; }

        int ReqPerDay { get; }
    }
}