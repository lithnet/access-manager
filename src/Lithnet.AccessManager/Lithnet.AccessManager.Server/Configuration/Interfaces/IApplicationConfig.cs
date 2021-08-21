using Lithnet.AccessManager.Api;

namespace Lithnet.AccessManager.Server.Configuration
{
    public interface IApplicationConfig
    {
        string Path { get; }

        string Hash { get; }

        LicensingOptions Licensing { get; set; }

        AuditOptions Auditing { get; set; }

        AzureAdOptions AzureAd { get; set; }

        AuthenticationOptions Authentication { get; set; }

        AuthorizationOptions Authorization { get; set; }

        EmailOptions Email { get; set; }

        AdminNotificationOptions AdminNotifications { get; set; }

        ForwardedHeadersAppOptions ForwardedHeaders { get; set; }

        RateLimitOptions RateLimits { get; set; }

        UserInterfaceOptions UserInterface { get; set; }

        DataProtectionOptions DataProtection { get; set; }

        JitConfigurationOptions JitConfiguration { get; set; }

        TokenIssuerOptions TokenIssuer { get; set; }

        PasswordPolicyOptions PasswordPolicy{ get; set; }

        ApiAuthenticationOptions ApiAuthentication { get; set; }

        DatabaseOptions Database { get; set; }
        
        void Save(string file, bool forceOverwrite);

        bool HasFileBeenModified();
    }
}