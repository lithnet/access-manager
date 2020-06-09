using Newtonsoft.Json;

namespace Lithnet.Laps.Web.Authorization
{
    public class JsonAce : IAce
    {
        public JsonAce(JsonAuditNotificationChannels recipients)
        {
            this.NotificationChannels = recipients;
        }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("sid")]
        public string Sid { get; set; }

        [JsonProperty("type")]
        public AceType Type { get; set; }

        [JsonProperty("notifications")]
        public IAuditNotificationChannels NotificationChannels { get; private set; }
    }
}