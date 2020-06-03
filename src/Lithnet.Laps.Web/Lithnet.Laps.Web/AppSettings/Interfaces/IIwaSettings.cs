using Microsoft.AspNetCore.Server.HttpSys;

namespace Lithnet.Laps.Web.AppSettings
{
    public interface IIwaSettings : IExternalAuthProviderSettings
    {
        AuthenticationSchemes AuthenticationSchemes { get; }
    }
}