using System.Collections.Generic;
using Newtonsoft.Json;

namespace Lithnet.Laps.Web.Authorization
{
    public class JsonEmailAuditRecipients
    {
        [JsonProperty("on-success")]
        public IList<string> SuccessRecipients { get; set; }

        [JsonProperty("on-failure")]
        public IList<string> FailureRecipients { get; set; }
    }
}