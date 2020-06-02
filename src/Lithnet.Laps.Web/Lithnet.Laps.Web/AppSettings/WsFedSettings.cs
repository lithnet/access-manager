using System;
using System.Security.Claims;
using Lithnet.Laps.Web.Internal;
using Microsoft.Extensions.Configuration;

namespace Lithnet.Laps.Web.AppSettings
{
    public class WsFedSettings : IWsFedSettings
    {
        private readonly IConfiguration configuration;

        public WsFedSettings(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public string Realm => this.configuration["authentication:wsfed:realm"];

        public string Metadata => this.configuration["authentication:wsfed:metadata"];

        public string ClaimName => this.configuration["authentication:wsfed:claim-name"] ?? ClaimTypes.Upn;

        public string UniqueClaimTypeIdentifier => this.configuration["authentication:oidc:unique-claim-type-identifier"] ?? ClaimTypes.PrimarySid;

        public string SignOutWReply => this.configuration["authentication:wsfed:signout-wreply"] ?? "/Home/LogOut";

        public bool CanLogout => true;

        public bool IdpLogout => this.configuration.GetValueOrDefault("authentication:wsfed:idp-logout", false);
    }
}