using Microsoft.AspNetCore.Server.HttpSys;

namespace Lithnet.Laps.Web.AppSettings
{
    public interface IIwaAuthenticationProvider : IHttpContextAuthenticationProvider
    {
        AuthenticationSchemes AuthenticationSchemes { get; }
    }
}