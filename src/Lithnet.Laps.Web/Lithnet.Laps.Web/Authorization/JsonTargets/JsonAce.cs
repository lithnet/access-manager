using Lithnet.Laps.Web.Internal;
using Newtonsoft.Json;

namespace Lithnet.Laps.Web.Authorization
{
    public class JsonAce : IAce
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("sid")]
        public string Sid { get; set; }

        [JsonProperty("type")]
        public AceType Type { get; set; }

        [JsonProperty("notifications")]
        [JsonConverter(typeof(JsonInterfaceConverter<JsonAuditNotificationChannels, IAuditNotificationChannels>))]
        public IAuditNotificationChannels NotificationChannels { get; private set; }
    }
}