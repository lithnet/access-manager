using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Lithnet.Laps.Web.Internal;
using Microsoft.AspNetCore.HttpOverrides;
using System.Net;

namespace Lithnet.Laps.Web.AppSettings
{
    public class XffHandlerSettings : IXffHandlerSettings
    {
        private readonly IConfiguration configuration;

        public XffHandlerSettings(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public string ForwardedForHeaderName => this.configuration["rate-limit:xff-ip-resolver:xff-header-name"] ?? ForwardedHeadersDefaults.XForwardedForHeaderName;

        public string ForwardedHostHeaderName => this.configuration["rate-limit:xff-ip-resolver:xfh-header-name"] ?? ForwardedHeadersDefaults.XForwardedHostHeaderName;

        public string ForwardedProtoHeaderName => this.configuration["rate-limit:xff-ip-resolver:xfp-header-name"] ?? ForwardedHeadersDefaults.XForwardedProtoHeaderName;

        public int ForwardLimit => this.configuration.GetValueOrDefault("rate-limit:xff-ip-resolver:forward-limit", 0);

        public IEnumerable<IPAddress> TrustedProxies
        {
            get
            {
                foreach (var item in this.configuration.GetSection("rate-limit:xff-ip-resolver:trusted-proxies")?.GetChildren())
                {
                    if (IPAddress.TryParse(item.Value, out IPAddress p))
                    {
                        yield return p;
                    }
                }
            }
        }

        public IEnumerable<IPNetwork> TrustedNetworks
        {
            get
            {
                foreach (var item in this.configuration.GetSection("rate-limit:xff-ip-resolver:trusted-networks")?.GetChildren())
                {
                    var knownNetworkParts = item.Value.Split('/');

                    if (knownNetworkParts.Length == 2)
                    {
                        if (IPAddress.TryParse(knownNetworkParts[0], out IPAddress ip))
                        {
                            if (int.TryParse(knownNetworkParts[1], out int mask))
                            {
                                yield return new IPNetwork(ip, mask);
                            }
                        }
                    }
                }
            }
        }
    }
}