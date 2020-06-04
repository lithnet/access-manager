using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Channels;
using Lithnet.Laps.Web.AppSettings;
using NLog;

namespace Lithnet.Laps.Web.Internal
{
    public class WebhookNotificationChannel : NotificationChannel<IWebhookChannelSettings>
    {
        private readonly IAuditSettings auditSettings;

        private readonly ITemplateProvider templates;

        public override string Name => "webhook";

        public WebhookNotificationChannel(ILogger logger, IAuditSettings auditSettings, ITemplateProvider templates, ChannelWriter<Action> queue)
            : base(logger, queue)
        {
            this.auditSettings = auditSettings;
            this.templates = templates;
        }

        public override void ProcessNotification(AuditableAction action, Dictionary<string, string> tokens, IImmutableSet<string> notificationChannels)
        {
            this.ProcessNotification(action, tokens, notificationChannels, this.auditSettings.Channels.Webhooks);
        }

        protected override void Send(AuditableAction action, Dictionary<string, string> tokens, IWebhookChannelSettings settings)
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