using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Lithnet.AccessManager.Server.Configuration
{
    public class AuthenticationOptions
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public AuthenticationMode Mode { get; set; }

        public List<string> AllowedPrincipals { get; set; } = new List<string>();

        public IwaAuthenticationProviderOptions Iwa { get; set; } 

        public OidcAuthenticationProviderOptions Oidc { get; set; } 

        public WsFedAuthenticationProviderOptions WsFed { get; set; }

        public CertificateAuthenticationProviderOptions ClientCert { get; set; }
    }
}