using Lithnet.Laps.Web.Internal;
using Microsoft.Extensions.Configuration;

namespace Lithnet.Laps.Web.AppSettings
{
    public class AuthenticationSettings : IAuthenticationSettings
    {
        private readonly IConfigurationRoot configuration;

        public AuthenticationSettings(IConfigurationRoot configuration)
        {
            this.configuration = configuration;
        }

        public string Mode => this.configuration["authentication:mode"] ?? "iwa";

        public bool ShowPii => this.configuration.GetValueOrDefault("authentication:debug:showpii", false);
    }
}