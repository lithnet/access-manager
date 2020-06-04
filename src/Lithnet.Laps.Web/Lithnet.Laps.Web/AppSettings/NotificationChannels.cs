using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Lithnet.Laps.Web.AppSettings
{
    public class NotificationChannels
    {
        private readonly IConfiguration config;

        public NotificationChannels(IConfiguration config)
        {
            this.config = config;
        }

        public IEnumerable<IWebhookChannelSettings> Webhooks
        {
            get
            {
                var sections = config.GetSection("webhook").GetChildren();

                foreach (var section in sections)
                {
                    yield return new WebhookChannelSettings(section);
                }
            }
        }

        public IEnumerable<IPowershellChannelSettings> Powershell
        {
            get
            {
                var sections = config.GetSection("powershell").GetChildren();

                foreach (var section in sections)
                {
                    yield return new PowershellChannelSettings(section);
                }
            }
        }

        public IEnumerable<ISmtpChannelSettings> Smtp
        {
            get
            {
                var sections = config.GetSection("smtp").GetChildren();

                foreach (var section in sections)
                {
                    yield return new SmtpChannelSettings(section);
                }
            }
        }
    }
}
