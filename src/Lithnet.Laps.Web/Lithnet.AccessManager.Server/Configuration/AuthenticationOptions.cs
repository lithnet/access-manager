namespace Lithnet.AccessManager.Configuration
{
    public class AuthenticationOptions
    {
        public AuthenticationMode Mode { get; set; }

        public bool ShowPii { get; set; }

        public OidcAuthenticationProviderOptions Oidc { get; set; }

        public WsFedAuthenticationProviderOptions WsFed { get; set; }

        public IwaAuthenticationProviderOptions Iwa { get; set; }
    }
}