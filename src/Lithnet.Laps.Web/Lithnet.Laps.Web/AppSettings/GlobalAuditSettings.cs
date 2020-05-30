using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using Lithnet.Laps.Web.Audit;
using Lithnet.Laps.Web.Config;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace Lithnet.Laps.Web.AppSettings
{
    public class GlobalAuditSettings : IAuditSettings
    {
        private IConfigurationRoot configuration;

        public GlobalAuditSettings(IConfigurationRoot configuration)
        {
            this.configuration = configuration;
        }

        public bool NotifySuccess => this.configuration.GetValue<bool>("auditing:emailOnSuccess");

        public bool NotifyFailure => this.configuration.GetValue<bool>("auditing:emailOnFailure");

        public IEnumerable<string> EmailAddresses
        {
            get
            {
                foreach (var item in this.configuration.GetSection("auditing:emailAddresses")?.GetChildren())
                {
                    yield return item.Value;
                }
            }
        }

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