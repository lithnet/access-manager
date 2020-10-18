using System.Security.Claims;

namespace Lithnet.AccessManager.Server.Configuration
{
    public class WsFedAuthenticationProviderOptions : AuthenticationProviderOptions
    {
        public string Metadata { get; set; }

        public string Realm { get; set; }

        public string ClaimName { get; set; }

        public bool IdpLogout { get; set; }
    }
}