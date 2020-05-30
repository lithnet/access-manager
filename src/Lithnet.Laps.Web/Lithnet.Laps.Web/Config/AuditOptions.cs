using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using Lithnet.Laps.Web.Audit;
using Newtonsoft.Json;

namespace Lithnet.Laps.Web.Config
{
    public class AuditOptions : IAuditSettings
    {
        [JsonProperty("emailOnSuccess")]
        public bool NotifySuccess { get; set; }

        [JsonProperty("emailOnFailure")]
        public bool NotifyFailure { get; set; }

        [JsonProperty("emailAddresses")]
        public IEnumerable<string> EmailAddresses { get; } = new List<string>();

        [JsonIgnore]
        public UsersToNotify UsersToNotify
        {
            get
            {
                UsersToNotify result = new UsersToNotify();

                if (this.NotifySuccess)
                {
                    result = result.NotifyOnSuccess(this.EmailAddresses.ToImmutableHashSet());
                }

                if (this.NotifyFailure)
                {
                    result = result.NotifyOnFailure(this.EmailAddresses.ToImmutableHashSet());
                }

                return result;
            }
        }
    }
}