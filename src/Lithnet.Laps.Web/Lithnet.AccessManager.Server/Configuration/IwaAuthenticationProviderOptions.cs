using System.Security.Claims;
using Lithnet.AccessManager.Server.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Lithnet.AccessManager.Server.Configuration
{
    public class IwaAuthenticationProviderOptions : AuthenticationProviderOptions
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public AuthenticationSchemes AuthenticationSchemes { get; set; } = AuthenticationSchemes.Negotiate;
        
        public override string ClaimName { get; set; } = ClaimTypes.PrimarySid;

        public override bool IdpLogout { get; set; } = false;
    }
}