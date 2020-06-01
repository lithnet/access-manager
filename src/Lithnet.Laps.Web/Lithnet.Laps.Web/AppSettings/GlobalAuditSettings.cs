using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace Lithnet.Laps.Web.AppSettings
{
    public class GlobalAuditSettings
    {
        private readonly IConfiguration configuration;

        public GlobalAuditSettings(IConfiguration configuration)
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