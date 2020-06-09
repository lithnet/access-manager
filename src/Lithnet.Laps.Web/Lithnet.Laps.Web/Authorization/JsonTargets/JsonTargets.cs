using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Lithnet.Laps.Web.Authorization
{
    public class JsonTargets
    {
        public JsonTargets(IList<JsonTarget> targets)
        {
            this.Targets = targets?.Cast<IJsonTarget>()?.ToList();
        }

        [JsonProperty("targets")]
        public IList<IJsonTarget> Targets { get; set; }
    }
}