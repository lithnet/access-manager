using System.DirectoryServices.AccountManagement;

namespace Lithnet.Laps.Web.AppSettings
{
    public interface IExternalAuthenticationProvider
    {
        string ClaimName { get; }

        IdentityType ClaimType { get; }

        string UniqueClaimTypeIdentifier { get; }
    }
}