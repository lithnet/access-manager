using System;
using Lithnet.Laps.Web.AppSettings;
using Microsoft.Extensions.Configuration;

namespace Lithnet.Laps.Web
{
    public class IpResolverSettings : IIpResolverSettings
    {
        private IConfigurationRoot configuration;

        public IpResolverSettings(IConfigurationRoot configuration)
        {
            this.configuration = configuration;
        }

        public IpResolverMode Mode
        {
            get
            {
                string value = this.configuration["rate-limit:ip-resolver:mode"];

                if (Enum.TryParse(value, true, out IpResolverMode result))
                {
                    return result;
                }

                return IpResolverMode.Default;
            }
        }

        public IXffHandling Xff => new XffHandling(this.configuration);

        public IClientIpHandling ClientIP => new ClientIpHandling(this.configuration);
    }
}