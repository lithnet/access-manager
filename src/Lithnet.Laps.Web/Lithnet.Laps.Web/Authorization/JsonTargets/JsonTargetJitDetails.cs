using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lithnet.Laps.Web.Authorization
{
    public class JsonTargetJitDetails
    {
        [JsonProperty("enabled")]
        public bool Enabled { get; set; }

        [JsonProperty("trustee")]
        public string Trustee { get; set; }

        [JsonProperty("expire-after")]
        public TimeSpan ExpireAfter { get; set; }
    }
}
/*
 "jit": {
        "enabled": true,
        "trustee": "idmdev1\\JIT-PC1",
        "expire-after": "00:30:00"
      }
*/
