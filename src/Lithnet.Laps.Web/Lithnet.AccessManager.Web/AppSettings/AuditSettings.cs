using System.Collections.Generic;
using Lithnet.AccessManager.Web.Internal;
using Microsoft.Extensions.Configuration;

namespace Lithnet.AccessManager.Web.AppSettings
{
    public class AuditSettings : IAuditSettings
    {
        private readonly IConfiguration config;

        public AuditSettings(IConfiguration configuration)
        {
            this.config = configuration;
        }

        public NotificationChannels Channels => new NotificationChannels(this.config.GetSection("auditing:notification-channels"));

        public IEnumerable<string> SuccessChannels => this.config.GetValuesOrDefault("auditing:global-notifications:on-success");

        public IEnumerable<string> FailureChannels => this.config.GetValuesOrDefault("auditing:global-notifications:on-failure");
    }
}