using System.Security.Claims;
using Lithnet.Laps.Web.Internal;
using Microsoft.AspNetCore.Server.HttpSys;
using Microsoft.Extensions.Configuration;

namespace Lithnet.Laps.Web.AppSettings
{
    public class IwaSettings : IIwaSettings
    {
        private readonly IConfiguration configuration;

        public IwaSettings(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public string UniqueClaimTypeIdentifier => this.configuration["authentication:iwa:unique-claim-type-identifier"] ?? ClaimTypes.PrimarySid;

        public string ClaimName => ClaimTypes.PrimarySid;

        public bool CanLogout => false;

        public bool IdpLogout => false;

        public AuthenticationSchemes AuthenticationSchemes => this.configuration.GetValueOrDefault("authentication:iwa:authentication-schemes", AuthenticationSchemes.Negotiate);
    }
}