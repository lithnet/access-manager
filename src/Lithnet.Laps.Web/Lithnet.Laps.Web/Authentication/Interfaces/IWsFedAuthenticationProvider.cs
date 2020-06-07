namespace Lithnet.Laps.Web.AppSettings
{
    public interface IWsFedAuthenticationProvider : IIdpAuthenticationProvider
    {
        string Metadata { get; }

        string Realm { get; }

        string SignOutWReply { get; }
    }
}