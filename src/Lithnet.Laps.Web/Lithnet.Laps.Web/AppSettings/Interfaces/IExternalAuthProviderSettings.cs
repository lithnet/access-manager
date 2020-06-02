namespace Lithnet.Laps.Web.AppSettings
{
    public interface IExternalAuthProviderSettings
    {
        string ClaimName { get; }

        string UniqueClaimTypeIdentifier { get; }

        bool CanLogout { get;  }

        bool IdpLogout { get; }
    }
}