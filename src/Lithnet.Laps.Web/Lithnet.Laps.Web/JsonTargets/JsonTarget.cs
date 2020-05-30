using System;
using System.Collections.Generic;
using Lithnet.Laps.Web.JsonTargets;
using Lithnet.Laps.Web.Models;
using Newtonsoft.Json;

namespace Lithnet.Laps.Web
{
    public class JsonTarget 
    {
        [JsonProperty("type")]
        public TargetType Type { get; private set; }

        [JsonProperty("name")]
        public string Name { get; private set; }

        [JsonProperty("sid")]
        public string Sid { get; private set; }

        [JsonProperty("expire-after")]
        public TimeSpan ExpireAfter { get; private set; }

        [JsonProperty("email-auditing")]
        public JsonEmailAuditRecipients EmailAuditing { get; private set; }

        [JsonProperty("acl")]
        public List<JsonAce> Acl { get; private set; }
    }
}