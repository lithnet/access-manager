using System.DirectoryServices.AccountManagement;

namespace Lithnet.Laps.Web.AppSettings
{
    public interface IWsFedSettings : IExternalAuthenticationProvider
    {
        string Metadata { get; }

        string Realm { get; }

        string SignOutWReply { get; }
    }
}