using System.DirectoryServices.AccountManagement;

namespace Lithnet.Laps.Web.AppSettings
{
    public interface IExternalAuthProviderSettings
    {
        string ClaimName { get; }

        IdentityType ClaimType { get; }

        string UniqueClaimTypeIdentifier { get; }
    }
}