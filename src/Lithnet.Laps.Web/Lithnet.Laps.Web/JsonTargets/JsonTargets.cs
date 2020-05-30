using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;

namespace Lithnet.Laps.Web.JsonTargets
{
    public class JsonTargets
    {
        [JsonProperty("targets")]
        public IList<JsonTarget> Targets { get; set; }
    }
}