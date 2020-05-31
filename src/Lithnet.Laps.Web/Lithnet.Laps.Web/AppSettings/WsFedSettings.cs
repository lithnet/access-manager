using System;
using System.DirectoryServices.AccountManagement;
using System.Security.Claims;
using Lithnet.Laps.Web.Internal;
using Microsoft.Extensions.Configuration;

namespace Lithnet.Laps.Web.AppSettings
{
    public class WsFedSettings : IWsFedSettings
    {
        private readonly IConfigurationRoot configuration;

        public WsFedSettings(IConfigurationRoot configuration)
        {
            this.configuration = configuration;
        }

        public string Realm => this.configuration["authentication:wsfed:realm"];

        public string Metadata => this.configuration["authentication:wsfed:metadata"];

        public string ClaimName => this.configuration["authentication:wsfed:claim-name"] ?? ClaimTypes.Upn;

        public IdentityType ClaimType => this.configuration.GetValueOrDefault("authentication:oidc:claim-type", IdentityType.UserPrincipalName);

        public string UniqueClaimTypeIdentifier => this.configuration["authentication:oidc:unique-claim-type-identifier"] ?? ClaimTypes.PrimarySid;

        public string SignOutWReply
        {
            get
            {
                return this.configuration["authentication:wsfed:signout-wreply"] ?? new Uri(new Uri(this.configuration["authentication:wsfed:realm"]?.Trim('/', '\\')), "Home/LogOut").ToString();
            }
        }
    }
}