using System.ComponentModel;
using System.Configuration;
using Newtonsoft.Json;

namespace Lithnet.Laps.Web
{
    public class RateLimits : IRateLimits
    {
        [JsonProperty("per-ip", DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue(true)]
        public RateLimitDetails PerIP { get; set; }

        [JsonProperty("per-user", DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue(true)]
        public RateLimitDetails PerUser { get; set; }
    }
}