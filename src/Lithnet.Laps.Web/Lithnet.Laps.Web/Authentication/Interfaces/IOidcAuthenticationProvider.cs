namespace Lithnet.Laps.Web.AppSettings
{
    public interface IOidcAuthenticationProvider : IIdpAuthenticationProvider
    {
        string Authority { get; }

        string ClientID { get; }

        string PostLogoutRedirectUri { get; }

        string RedirectUri { get; }

        string ResponseType { get; }

        string Secret { get; }
    }
}