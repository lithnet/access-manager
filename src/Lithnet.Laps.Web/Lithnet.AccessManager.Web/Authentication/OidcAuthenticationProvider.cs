using System.Security.Claims;
using Lithnet.AccessManager.Web.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using NLog;

namespace Lithnet.AccessManager.Web.AppSettings
{
    public class OidcAuthenticationProvider : IdpAuthenticationProvider, IOidcAuthenticationProvider
    {
        private readonly IConfiguration configuration;

        public OidcAuthenticationProvider(IConfiguration configuration, ILogger logger, IDirectory directory, IHttpContextAccessor httpContextAccessor)
            : base(logger, directory, httpContextAccessor)
        {
            this.configuration = configuration;
        }

        public string ClientID => this.configuration["authentication:oidc:client-id"];

        public string Secret => this.configuration["authentication:oidc:secret"];

        public string RedirectUri => this.configuration["authentication:oidc:redirect-uri"];

        public string Authority => this.configuration["authentication:oidc:authority"].TrimEnd('/');

        public override string ClaimName => this.configuration["authentication:oidc:claim-name"] ?? ClaimTypes.Upn;

        public override string UniqueClaimTypeIdentifier => this.configuration["authentication:oidc:unique-claim-type-identifier"] ?? ClaimTypes.PrimarySid;

        public string ResponseType => this.configuration["authentication:oidc:response-type"] ?? OpenIdConnectResponseType.CodeIdToken;

        public string PostLogoutRedirectUri => this.configuration["authentication:oidc:post-logout-redirect-uri"] ?? "/Home/LoggedOut";

        public override bool CanLogout => true;

        public override bool IdpLogout => this.configuration.GetValueOrDefault("authentication:oidc:idp-logout", false);
    }
}