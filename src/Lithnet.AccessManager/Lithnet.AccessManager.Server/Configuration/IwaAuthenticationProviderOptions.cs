using System.Security.Claims;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Lithnet.AccessManager.Server.Configuration
{
    public class IwaAuthenticationProviderOptions : AuthenticationProviderOptions
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public AuthenticationSchemes AuthenticationSchemes { get; set; } = AuthenticationSchemes.Negotiate;
    }
}