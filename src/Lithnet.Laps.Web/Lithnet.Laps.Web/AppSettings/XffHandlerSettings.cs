using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Lithnet.Laps.Web.Internal;

namespace Lithnet.Laps.Web.AppSettings
{
    public class XffHandlerSettings : IXffHandlerSettings
    {
        private readonly IConfiguration configuration;

        public XffHandlerSettings(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public string HeaderName => this.configuration["rate-limit:ip-resolver:xff:header-name"] ?? "X-Forwarded-For";

        public int ProxyDepth => this.configuration.GetValueOrDefault("rate-limit:ip-resolver:xff:proxy-depth", 0, 0);

        public XffResolverMode Mode => this.configuration.GetValueOrDefault("rate-limit:ip-resolver:xff:mode", XffResolverMode.ProxyDepth);

        public IEnumerable<string> TrustedProxies
        {
            get
            {
                foreach (var item in this.configuration.GetSection("rate-limit:ip-resolver:xff:trusted-proxies")?.GetChildren())
                {
                    yield return item.Value;
                }
            }
        }
    }
}