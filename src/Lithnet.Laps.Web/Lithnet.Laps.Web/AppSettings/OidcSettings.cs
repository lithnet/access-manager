using System;
using System.DirectoryServices.AccountManagement;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Lithnet.Laps.Web.AppSettings
{
    public class OidcSettings : IOidcSettings
    {
        private IConfigurationRoot configuration;

        public OidcSettings(IConfigurationRoot configuration)
        {
            this.configuration = configuration;
        }

        public string ClientID => this.configuration["authentication:oidc:clientid"];

        public string Secret => this.configuration["authentication:oidc:secret"];

        public string RedirectUri => this.configuration["authentication:oidc:redirecturi"];

        public string Authority => this.configuration["authentication:oidc:authority"].TrimEnd('/');

        public string ClaimName => this.configuration["authentication:oidc:claimName"] ?? ClaimTypes.Upn;

        public IdentityType ClaimType
        {
            get
            {
                if (Enum.TryParse(this.configuration["authentication:oidc:claimType"], out IdentityType claimType))
                {
                    return claimType;
                }
                else
                {
                    return IdentityType.UserPrincipalName;
                }
            }
        }

        public string UniqueClaimTypeIdentifier => this.configuration["authentication:oidc:uniqueClaimTypeIdentifier"] ?? ClaimTypes.PrimarySid;

        public string ResponseType => this.configuration["authentication:oidc:responseType"] ?? OpenIdConnectResponseType.IdToken;

        public string PostLogourRedirectUri
        {
            get
            {
                return this.configuration["authentication:oidc:postLogoutRedirectUri"] ?? new Uri(new Uri(this.configuration["authentication:oidc:redirecturi"]?.Trim('/', '\\')), "Home/LogOut").ToString();
            }
        }
    }
}