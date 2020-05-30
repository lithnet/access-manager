using System;
using System.Collections.Generic;
using Lithnet.Laps.Web.AppSettings;
using Microsoft.Extensions.Configuration;

namespace Lithnet.Laps.Web
{
    public class XffHandling : IXffHandling
    {
        private IConfigurationRoot configuration;

        public XffHandling(IConfigurationRoot configuration)
        {
            this.configuration = configuration;
        }

        public string HeaderName => this.configuration["rate-limit:ip-resolver:xff:header-name"] ?? "X-Forwarded-For";

        public int ProxyDepth
        {
            get
            {
                string value = this.configuration["rate-limit:ip-resolver:xff:proxy-depth"];

                if (int.TryParse(value, out int result))
                {
                    return Math.Max(0, result);
                }

                return 0;
            }
        }

        public XffResolverMode Mode
        {
            get
            {
                string value = this.configuration["rate-limit:ip-resolver:xff:mode"];

                if (Enum.TryParse(value, true, out XffResolverMode result))
                {
                    return result;
                }

                return XffResolverMode.ProxyDepth;
            }
        }

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