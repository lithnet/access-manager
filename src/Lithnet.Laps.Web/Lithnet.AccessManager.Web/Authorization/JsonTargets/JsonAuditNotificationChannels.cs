using System.Collections.Generic;
using Newtonsoft.Json;

namespace Lithnet.AccessManager.Web.Authorization
{
    public class JsonAuditNotificationChannels : IAuditNotificationChannels
    {
        [JsonProperty("on-success")]
        public IList<string> OnSuccess { get; set; }

        [JsonProperty("on-failure")]
        public IList<string> OnFailure { get; set; }
    }
}