using System.Collections.Generic;
using Lithnet.AccessManager.Server.Configuration;
using Lithnet.AccessManager.Web.Internal;
using Newtonsoft.Json;

namespace Lithnet.AccessManager.Web.Authorization
{
    public class JsonTarget : IJsonTarget
    {
        [JsonProperty("type")]
        public TargetType Type { get; private set; }

        [JsonProperty("name")]
        public string Name { get; private set; }

        [JsonProperty("sid")]
        public string Sid { get; private set; }

        [JsonProperty("jit")]
        public JsonTargetJitDetails Jit { get; set; } = new JsonTargetJitDetails();

        [JsonProperty("laps")]
        public JsonTargetLapsDetails Laps { get; set; } = new JsonTargetLapsDetails();

        [JsonProperty("notifications")]
        [JsonConverter(typeof(JsonInterfaceConverter<JsonAuditNotificationChannels, IAuditNotificationChannels>))]
        public IAuditNotificationChannels NotificationChannels { get; private set; }

        [JsonProperty("acl")]
        [JsonConverter(typeof(JsonListInterfaceConverter<JsonAce, IAce>))]
        public IList<IAce> Acl { get; private set; }
    }
}

/* {
      "name": "IDMDEV1\\PC1",
      "type": "computer",
      "expire-after": "02:00:00",
      "acl": [
        {
          "name": "idmdev1\\domain admins",
          "type": "allow"
        },
        {
          "name": "idmdev1\\ryan",
          "type": "deny"
        }
      ],
      "notifications": {
        "on-success": [ "email-domain-admins" ],
        "on-failure": [ "email-domain-admins" ]
      },
      "jit": {
        "enabled": true,
        "trustee": "idmdev1\\JIT-PC1",
        "expire-after": "00:30:00"
      },
      "laps": {
        "enabled": true,
        "expire-after": "02:00:00"
      }
    },*/
