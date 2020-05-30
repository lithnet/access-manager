using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Web;
using Lithnet.Laps.Web.Audit;
using Lithnet.Laps.Web.Config;
using Newtonsoft.Json;

namespace Lithnet.Laps.Web.JsonTargets
{
    public class JsonEmailAuditRecipients
    {
        [JsonProperty("on-success")]
        public IList<string> SuccessRecipients { get; set; }

        [JsonProperty("on-failure")]
        public IList<string> FailureRecipients { get; set; }
    }
}