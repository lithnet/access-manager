using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Lithnet.Laps.Web.Authorization
{
    public class JsonTarget : IJsonTarget
    {
        public JsonTarget (JsonAuditNotificationChannels channels, IList<JsonAce> acl)
        {
            this.NotificationChannels = channels;
            this.Acl = acl?.Cast<IAce>()?.ToList();
        }

        [JsonProperty("type")]
        public TargetType Type { get; private set; }

        [JsonProperty("name")]
        public string Name { get; private set; }

        [JsonProperty("sid")]
        public string Sid { get; private set; }

        [JsonProperty("expire-after")]
        public TimeSpan ExpireAfter { get; private set; }

        [JsonProperty("notifications")]
        public IAuditNotificationChannels NotificationChannels { get; private set; }

        [JsonProperty("acl")]
        public IList<IAce> Acl { get; private set; }
    }
}