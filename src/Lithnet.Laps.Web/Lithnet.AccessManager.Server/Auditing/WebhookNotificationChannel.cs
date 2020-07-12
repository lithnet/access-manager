using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Channels;
using Lithnet.AccessManager.Server.Configuration;
using Microsoft.Extensions.Options;
using NLog;

namespace Lithnet.AccessManager.Server.Auditing
{
    public class WebhookNotificationChannel : NotificationChannel<WebhookNotificationChannelDefinition>
    {
        private readonly ITemplateProvider templates;

        public override string Name => "webhook";

        protected override IList<WebhookNotificationChannelDefinition> NotificationChannelDefinitions { get; }

        public WebhookNotificationChannel(ILogger logger, IOptions<AuditOptions> auditSettings, ITemplateProvider templates, ChannelWriter<Action> queue)
            : base(logger, queue)
        {
            this.NotificationChannelDefinitions = auditSettings.Value.NotificationChannels.Webhooks;
            this.templates = templates;
        }

        protected override void Send(AuditableAction action, Dictionary<string, string> tokens, WebhookNotificationChannelDefinition settings)
        {
            HttpClient client = new HttpClient();
            HttpRequestMessage message = new HttpRequestMessage();

            string content = action.IsSuccess ? templates.GetTemplate(settings.TemplateSuccess) : templates.GetTemplate(settings.TemplateFailure);

            content = TokenReplacer.ReplaceAsJson(tokens, content);

            message.Content = new StringContent(content, Encoding.UTF8, settings.ContentType);
            message.RequestUri = new Uri(settings.Url);
            message.Method = new HttpMethod(settings.HttpMethod);

            var response = client.SendAsync(message).GetAwaiter().GetResult();

            response.EnsureSuccessStatusCode();
        }
    }
}