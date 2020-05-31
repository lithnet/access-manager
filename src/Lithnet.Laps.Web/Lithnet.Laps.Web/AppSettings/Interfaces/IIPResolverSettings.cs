namespace Lithnet.Laps.Web.AppSettings
{
    public interface IIpResolverSettings
    {
        IClientIpHandlingSettings ClientIP { get; }

        IpResolverMode Mode { get; }

        IXffHandlerSettings Xff { get; }
    }
}