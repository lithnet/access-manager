using Microsoft.Extensions.Configuration;

namespace Lithnet.Laps.Web.AppSettings
{
    public class AuthenticationSettings : IAuthenticationSettings
    {
        private IConfigurationRoot configuration;

        public AuthenticationSettings(IConfigurationRoot configuration)
        {
            this.configuration = configuration;
        }

        public string Mode => this.configuration["authentication:mode"] ?? "iwa";

        public bool ShowPii
        {
            get
            {
                string value = this.configuration["authentication:debug:showpii"];

                if (bool.TryParse(value, out bool b))
                {
                    return b;
                }

                return false;
            }
        }
    }
}