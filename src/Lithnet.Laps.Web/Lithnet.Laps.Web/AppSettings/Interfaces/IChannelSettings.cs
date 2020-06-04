namespace Lithnet.Laps.Web.AppSettings
{
    public interface IChannelSettings
    {
        bool Enabled { get; }

        string ID { get; }

        bool DenyOnAuditError { get; }
    }
}