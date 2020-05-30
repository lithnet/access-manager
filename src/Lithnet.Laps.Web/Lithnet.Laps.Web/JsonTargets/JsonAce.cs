using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Lithnet.Laps.Web.Audit;
using Lithnet.Laps.Web.Config;
using Lithnet.Laps.Web.JsonTargets;
using Lithnet.Laps.Web.Models;
using Newtonsoft.Json;

namespace Lithnet.Laps.Web
{
    public class JsonAce
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("sid")]
        public string Sid { get; set; }

        [JsonProperty("type")]
        public AceType Type { get; set; }

        [JsonProperty("email-auditing")]
        public JsonEmailAuditRecipients EmailAuditing { get; private set; }
    }
}