using System;
using System.Collections.Generic;
using System.Configuration.Internal;
using System.Linq;
using System.Threading.Tasks;
using Lithnet.Laps.Web.Internal;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Configuration;

namespace Lithnet.Laps.Web.AppSettings
{
    public class WebhookChannelSettings : IWebhookChannelSettings
    {
        private readonly IConfiguration config;

        public WebhookChannelSettings(IConfiguration config)
        {
            this.config = config;
        }

        public bool Enabled => this.config.GetValueOrDefault("enabled", false);

        public string ID => this.config["id"];

        public string TemplateSuccess => this.config["template-success"];

        public string TemplateFailure => this.config["template-failure"];

        public string Url => this.config["url"];

        public string HttpMethod => this.config["http-method"] ?? "POST";

        public string ContentType => this.config["content-type"] ?? "application/json";

        public bool DenyOnAuditError => this.config.GetValueOrDefault("deny-on-error", false);
    }
}
