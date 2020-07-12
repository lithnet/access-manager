using System.Collections;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Lithnet.AccessManager.Server.Configuration
{
    public class OidcAuthenticationProviderOptions : AuthenticationProviderOptions
    {
        public string Authority { get; set; }

        public string ClientID { get; set; }

        public string ResponseType { get; set; } = OpenIdConnectResponseType.CodeIdToken;

        public string Secret { get; set; }

        public override string ClaimName { get; set; } = ClaimTypes.Upn;

        public override bool IdpLogout { get; set; }

        public IList<string> Scopes { get; set; }
    }
}