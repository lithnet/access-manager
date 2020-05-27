using System.ComponentModel;
using System.Configuration;
using Newtonsoft.Json;

namespace Lithnet.Laps.Web
{
    public class RateLimitDetails : IRateLimitDetails
    {
        [JsonProperty("enabled", DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue(true)]
        public bool Enabled { get; set; }

        [JsonProperty("requestsPerMinute", DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue(10)]
        public int ReqPerMinute { get; set; }

        [JsonProperty("requestsPerHour", DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue(50)]
        public int ReqPerHour { get; set; }

        [JsonProperty("requestsPerDay", DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue(100)]
        public int ReqPerDay { get; set; }
    }
}