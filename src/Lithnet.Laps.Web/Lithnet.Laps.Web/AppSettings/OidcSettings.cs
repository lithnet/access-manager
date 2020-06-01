using System;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Lithnet.Laps.Web.AppSettings
{
    public class OidcSettings : IOidcSettings
    {
        private readonly IConfiguration configuration;

        public OidcSettings(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public string ClientID => this.configuration["authentication:oidc:client-id"];

        public string Secret => this.configuration["authentication:oidc:secret"];

        public string RedirectUri => this.configuration["authentication:oidc:redirect-uri"];

        public string Authority => this.configuration["authentication:oidc:authority"].TrimEnd('/');

        public string ClaimName => this.configuration["authentication:oidc:claim-name"] ?? ClaimTypes.Upn;

        public string UniqueClaimTypeIdentifier => this.configuration["authentication:oidc:unique-claim-type-identifier"] ?? ClaimTypes.PrimarySid;

        public string ResponseType => this.configuration["authentication:oidc:response-type"] ?? OpenIdConnectResponseType.CodeIdToken;

        public string PostLogourRedirectUri
        {
            get
            {
                return this.configuration["authentication:oidc:post-logout-redirect-uri"] ?? new Uri(new Uri(this.configuration["authentication:oidc:redirect-uri"]?.Trim('/', '\\')), "Home/LogOut").ToString();
            }
        }
    }
}