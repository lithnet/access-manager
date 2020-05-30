using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using Lithnet.Laps.Web.Audit;
using Lithnet.Laps.Web.Config;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace Lithnet.Laps.Web.AppSettings
{
    public class GlobalAuditSettings
    {
        private IConfigurationRoot configuration;

        public GlobalAuditSettings(IConfigurationRoot configuration)
        {
            this.configuration = configuration;
        }

        public IEnumerable<string> SuccessRecipients
        {
            get
            {
                foreach (var item in this.configuration.GetSection("email-auditing:on-success")?.GetChildren())
                {
                    yield return item.Value;
                }
            }
        }

        public IEnumerable<string> FailureRecipients
        {
            get
            {
                foreach (var item in this.configuration.GetSection("email-auditing:on-failure")?.GetChildren())
                {
                    yield return item.Value;
                }
            }
        }
    }
}