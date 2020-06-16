using System.Security.Claims;
using Lithnet.AccessManager.Web.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using NLog;

namespace Lithnet.AccessManager.Web.AppSettings
{
    public class WsFedAuthenticationProvider : IdpAuthenticationProvider, IWsFedAuthenticationProvider
    {
        private readonly IConfiguration configuration;

        public WsFedAuthenticationProvider(IConfiguration configuration, ILogger logger, IDirectory directory, IHttpContextAccessor httpContextAccessor)
            :base (logger, directory, httpContextAccessor)
        {
            this.configuration = configuration;
        }

        public string Realm => this.configuration["authentication:wsfed:realm"];

        public string Metadata => this.configuration["authentication:wsfed:metadata"];

        public override string ClaimName => this.configuration["authentication:wsfed:claim-name"] ?? ClaimTypes.Upn;

        public override string UniqueClaimTypeIdentifier => this.configuration["authentication:oidc:unique-claim-type-identifier"] ?? ClaimTypes.PrimarySid;

        public string SignOutWReply => this.configuration["authentication:wsfed:signout-wreply"] ?? "/Home/LogOut";

        public override bool CanLogout => true;

        public override bool IdpLogout => this.configuration.GetValueOrDefault("authentication:wsfed:idp-logout", false);
    }
}