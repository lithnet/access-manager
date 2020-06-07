using Lithnet.Laps.Web.ActiveDirectory;

namespace Lithnet.Laps.Web.AppSettings
{
    public interface IAuthenticationProvider
    {
        string ClaimName { get; }

        string UniqueClaimTypeIdentifier { get; }

        bool CanLogout { get;  }

        bool IdpLogout { get; }

        IUser GetLoggedInUser();
    }
}