using System.DirectoryServices.AccountManagement;

namespace Lithnet.Laps.Web.AppSettings
{
    public interface IOidcSettings : IExternalAuthenticationProvider
    {
        string Authority { get; }

        string ClientID { get; }

        string PostLogourRedirectUri { get; }

        string RedirectUri { get; }

        string ResponseType { get; }

        string Secret { get; }
    }
}