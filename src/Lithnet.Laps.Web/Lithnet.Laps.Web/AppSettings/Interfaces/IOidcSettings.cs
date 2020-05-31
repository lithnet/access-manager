namespace Lithnet.Laps.Web.AppSettings
{
    public interface IOidcSettings : IExternalAuthProviderSettings
    {
        string Authority { get; }

        string ClientID { get; }

        string PostLogourRedirectUri { get; }

        string RedirectUri { get; }

        string ResponseType { get; }

        string Secret { get; }
    }
}