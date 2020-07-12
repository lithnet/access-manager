using System.Security.Claims;

namespace Lithnet.AccessManager.Server.Configuration
{
    public class WsFedAuthenticationProviderOptions : AuthenticationProviderOptions
    {
        public string Metadata { get; set; }

        public string Realm { get; set; }

        public override string ClaimName { get; set; } = ClaimTypes.Upn;

        public override bool IdpLogout { get; set; }
    }
}