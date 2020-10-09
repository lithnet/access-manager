using System.Collections.Generic;

namespace Lithnet.AccessManager.Server.Configuration
{
    public class OidcAuthenticationProviderOptions : AuthenticationProviderOptions
    {
        public string Authority { get; set; }

        public string ClientID { get; set; }

        public string ResponseType { get; set; }

        public EncryptedData Secret { get; set; }

        public string ClaimName { get; set; }

        public bool IdpLogout { get; set; }

        public bool? GetUserInfoEndpointClaims { get; set; }
        
        public IList<string> Scopes { get; set; } 

        public Dictionary<string, string > ClaimMapping { get; set; }
    }
}