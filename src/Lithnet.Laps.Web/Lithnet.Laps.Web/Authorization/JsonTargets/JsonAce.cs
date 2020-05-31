using Newtonsoft.Json;

namespace Lithnet.Laps.Web.Authorization
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