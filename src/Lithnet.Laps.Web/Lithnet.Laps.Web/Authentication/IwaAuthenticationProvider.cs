using System.Security.Claims;
using Lithnet.Laps.Web.ActiveDirectory;
using Lithnet.Laps.Web.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.HttpSys;
using Microsoft.Extensions.Configuration;

namespace Lithnet.Laps.Web.AppSettings
{
    public class IwaAuthenticationProvider : HttpContextAuthenticationProvider, IIwaAuthenticationProvider
    {
        private readonly IConfiguration configuration;

        public IwaAuthenticationProvider(IConfiguration configuration, IDirectory directory, IHttpContextAccessor httpContextAccessor)
            :base (httpContextAccessor, directory)
        {
            this.configuration = configuration;
        }

        public override string UniqueClaimTypeIdentifier => this.configuration["authentication:iwa:unique-claim-type-identifier"] ?? ClaimTypes.PrimarySid;

        public override string ClaimName => ClaimTypes.PrimarySid;

        public override bool CanLogout => false;

        public override bool IdpLogout => false;

        public AuthenticationSchemes AuthenticationSchemes => this.configuration.GetValueOrDefault("authentication:iwa:authentication-schemes", AuthenticationSchemes.Negotiate);
    }
}