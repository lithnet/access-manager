using System;
using System.DirectoryServices.AccountManagement;
using System.Security.Claims;
using Lithnet.Laps.Web.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Lithnet.Laps.Web.AppSettings
{
    public class OidcSettings : IOidcSettings
    {
        private readonly IConfigurationRoot configuration;

        public OidcSettings(IConfigurationRoot configuration)
        {
            this.configuration = configuration;
        }

        public string ClientID => this.configuration["authentication:oidc:client-id"];

        public string Secret => this.configuration["authentication:oidc:secret"];

        public string RedirectUri => this.configuration["authentication:oidc:redirect-uri"];

        public string Authority => this.configuration["authentication:oidc:authority"].TrimEnd('/');

        public string ClaimName => this.configuration["authentication:oidc:claim-name"] ?? ClaimTypes.Upn;

        public IdentityType ClaimType => this.configuration.GetValueOrDefault("authentication:oidc:claim-type", IdentityType.UserPrincipalName);

        public string UniqueClaimTypeIdentifier => this.configuration["authentication:oidc:unique-claim-type-identifier"] ?? ClaimTypes.PrimarySid;

        public string ResponseType => this.configuration["authentication:oidc:response-type"] ?? OpenIdConnectResponseType.IdToken;

        public string PostLogourRedirectUri
        {
            get
            {
                return this.configuration["authentication:oidc:post-logout-redirect-uri"] ?? new Uri(new Uri(this.configuration["authentication:oidc:redirect-uri"]?.Trim('/', '\\')), "Home/LogOut").ToString();
            }
        }
    }
}