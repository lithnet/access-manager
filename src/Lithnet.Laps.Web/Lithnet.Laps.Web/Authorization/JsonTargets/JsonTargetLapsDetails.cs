using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lithnet.Laps.Web.Authorization
{
    public class JsonTargetLapsDetails
    {
        [JsonProperty("enabled")]
        public bool Enabled { get; set; }

        [JsonProperty("expire-after")]
        public TimeSpan ExpireAfter { get; set; }
    }
}
