using System;
using Newtonsoft.Json;

namespace Lithnet.AccessManager.Web.Authorization
{
    public class JsonTargetJitDetails
    {
        [JsonProperty("trustee")]
        public string AuthorizingGroup { get; set; }

        [JsonProperty("expire-after")]
        public TimeSpan ExpireAfter { get; set; }
    }
}