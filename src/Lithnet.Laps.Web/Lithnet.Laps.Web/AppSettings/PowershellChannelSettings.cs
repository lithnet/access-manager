using System.Collections.Generic;
using Lithnet.Laps.Web.Internal;
using Microsoft.Extensions.Configuration;

namespace Lithnet.Laps.Web.AppSettings
{
    public class PowershellChannelSettings : IPowershellChannelSettings
    {
        private readonly IConfiguration config;

        public PowershellChannelSettings(IConfiguration config)
        {
            this.config = config;
        }

        public bool Enabled => this.config.GetValueOrDefault("enabled", false);

        public string ID => this.config["id"];

        public string Script => this.config["script"];

        public int TimeOut => this.config.GetValueOrDefault("script-timeout", 10);

        public bool DenyOnAuditError => this.config.GetValueOrDefault("deny-on-error", false);
    }
}
