using Lithnet.Laps.Web.AppSettings;

namespace Lithnet.Laps.Web
{
    public interface IIpResolverSettings
    {
        IClientIpHandling ClientIP { get; }

        IpResolverMode Mode { get; }

        IXffHandling Xff { get; }
    }
}