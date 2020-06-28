using System.Security.Claims;
using Microsoft.AspNetCore.Server.HttpSys;

namespace Lithnet.AccessManager.Configuration
{
    public class IwaAuthenticationProviderOptions : AuthenticationProviderOptions
    {
        public AuthenticationSchemes AuthenticationSchemes { get; set; }
        
        public override string ClaimName { get; set; } = ClaimTypes.PrimarySid;

        public override bool IdpLogout { get; set; } = false;
    }
}