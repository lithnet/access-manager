using Lithnet.Laps.Web.Internal;
using Microsoft.Extensions.Configuration;

namespace Lithnet.Laps.Web.AppSettings
{
    public class EmailSettings : IEmailSettings
    {
        private readonly IConfigurationRoot configuration;

        public EmailSettings(IConfigurationRoot configuration)
        {
            this.configuration = configuration;
        }

        public bool IsConfigured => !string.IsNullOrWhiteSpace(this.Host);

        public string Host => this.configuration["email:host"];

        public int Port => this.configuration.GetValueOrDefault("email:port", 1, 25);

        public bool UseSsl => this.configuration.GetValueOrDefault("email:use-ssl", false);

        public bool UseDefaultCredentials => this.configuration.GetValueOrDefault("email:use-default-credentials", false);

        public string Username => this.configuration["email:username"];

        public string Password => this.configuration["email:password"];

        public string FromAddress => this.configuration["email:from-address"];
    }
}