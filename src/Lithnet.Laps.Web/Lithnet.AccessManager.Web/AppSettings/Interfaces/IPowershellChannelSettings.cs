namespace Lithnet.AccessManager.Web.AppSettings
{
    public interface IPowershellChannelSettings : IChannelSettings
    {
        string Script { get; }

        int TimeOut { get; }
    }
}