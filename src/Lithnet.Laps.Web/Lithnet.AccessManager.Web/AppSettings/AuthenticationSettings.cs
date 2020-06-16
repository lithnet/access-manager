using Lithnet.AccessManager.Web.Internal;
using Microsoft.Extensions.Configuration;

namespace Lithnet.AccessManager.Web.AppSettings
{
    public class AuthenticationSettings : IAuthenticationSettings
    {
        private readonly IConfiguration configuration;

        public AuthenticationSettings(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public string Mode => this.configuration["authentication:mode"] ?? "iwa";

        public bool ShowPii => this.configuration.GetValueOrDefault("authentication:debug:showpii", false);
    }
}