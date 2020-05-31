namespace Lithnet.Laps.Web.AppSettings
{
    public interface IWsFedSettings : IExternalAuthProviderSettings
    {
        string Metadata { get; }

        string Realm { get; }

        string SignOutWReply { get; }
    }
}