using Lithnet.AccessManager.Configuration;
using Microsoft.AspNetCore.Builder;

namespace Lithnet.AccessManager.Server.Configuration
{
    public interface IApplicationConfig
    {
        AuditOptions Auditing { get; set; }

        AuthenticationOptions Authentication { get; set; }

        AuthorizationOptions Authorization { get; set; }

        EmailOptions Email { get; set; }

        ForwardedHeadersAppOptions ForwardedHeaders { get; set; }

        HostingOptions Hosting { get; set; }

        RateLimitOptions RateLimits { get; set; }

        UserInterfaceOptions UserInterface { get; set; }

        void Save(string file);
    }
}