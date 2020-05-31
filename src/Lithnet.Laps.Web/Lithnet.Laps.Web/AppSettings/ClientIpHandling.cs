using Microsoft.Extensions.Configuration;

namespace Lithnet.Laps.Web.AppSettings
{
    public class ClientIpHandling : IClientIpHandlingSettings
    {
        private IConfigurationRoot configuration;

        public ClientIpHandling(IConfigurationRoot configuration)
        {
            this.configuration = configuration;
        }

        public string HeaderName => this.configuration["rate-limit:ip-resolver:client-ip:header-name"] ?? "Client-IP";
    }
}