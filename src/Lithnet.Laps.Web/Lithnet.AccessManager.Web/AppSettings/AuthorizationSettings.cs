using Lithnet.AccessManager.Web.Internal;
using Microsoft.Extensions.Configuration;

namespace Lithnet.AccessManager.Web.AppSettings
{
    public class AuthorizationSettings : IAuthorizationSettings
    {
        private readonly IConfiguration configuration;

        public AuthorizationSettings(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public bool JsonProviderEnabled => this.configuration.GetValueOrDefault("authorization:json-provider:enabled", true);

        public bool PowershellProviderEnabled => this.configuration.GetValueOrDefault("authorization:powershell-provider:enabled", true);

        public string PowershellScriptFile => this.configuration["authorization:powershell-provider:script-file"];

        public string JsonAuthorizationFile => this.configuration["authorization:json-provider:authorization-file"];

        public int PowershellScriptTimeout => this.configuration.GetValueOrDefault("authorization:powershell-provider:timeout", 20);
    }
}