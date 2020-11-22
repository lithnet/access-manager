using Lithnet.AccessManager.Enterprise;

namespace Lithnet.AccessManager.Server.Configuration
{
    public interface IApplicationConfig
    {
        string Path { get; }

        LicensingOptions Licensing { get; set; }

        AuditOptions Auditing { get; set; }

        AuthenticationOptions Authentication { get; set; }

        AuthorizationOptions Authorization { get; set; }

        EmailOptions Email { get; set; }

        ForwardedHeadersAppOptions ForwardedHeaders { get; set; }

        RateLimitOptions RateLimits { get; set; }

        UserInterfaceOptions UserInterface { get; set; }

        DatabaseConfigurationOptions DatabaseConfiguration { get; set; }

        DataProtectionOptions DataProtection { get; set; }

        JitConfigurationOptions JitConfiguration { get; set; }

        void Save(string file);
    }
}