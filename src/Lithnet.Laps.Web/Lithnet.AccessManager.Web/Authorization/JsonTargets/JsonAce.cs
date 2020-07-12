using Lithnet.AccessManager.Server;
using Lithnet.AccessManager.Server.Configuration;
using Lithnet.AccessManager.Web.Internal;
using Newtonsoft.Json;

namespace Lithnet.AccessManager.Web.Authorization
{
    public class JsonAce : IAce
    {
        [JsonProperty("trustee")]
        public string Trustee { get; set; }

        [JsonProperty("sid")]
        public string Sid { get; set; }

        [JsonProperty("type")]
        public AceType Type { get; set; }

        [JsonProperty("access")]
        public AccessMask Access { get; set; }

        [JsonProperty("notifications")]
        [JsonConverter(typeof(JsonInterfaceConverter<JsonAuditNotificationChannels, IAuditNotificationChannels>))]
        public IAuditNotificationChannels NotificationChannels { get; private set; }
    }
}