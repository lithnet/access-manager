using System.Collections.Generic;
using Newtonsoft.Json;

namespace Lithnet.Laps.Web.Authorization
{
    public class JsonTargets
    {
        [JsonProperty("targets")]
        public IList<JsonTarget> Targets { get; set; }
    }
}