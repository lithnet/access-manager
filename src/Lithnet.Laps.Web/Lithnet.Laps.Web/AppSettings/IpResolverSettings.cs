using System;
using Lithnet.Laps.Web.Internal;
using Microsoft.Extensions.Configuration;

namespace Lithnet.Laps.Web.AppSettings
{
    public class IpResolverSettings : IIpResolverSettings
    {
        private readonly IConfiguration configuration;

        public IpResolverSettings(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public IpResolverMode Mode  => this.configuration.GetValueOrDefault("rate-limit:ip-resolver:mode", IpResolverMode.Default);

        public IXffHandlerSettings Xff => new XffHandlerSettings(this.configuration);

        public IClientIpHandlingSettings ClientIP => new ClientIpHandling(this.configuration);
    }
}