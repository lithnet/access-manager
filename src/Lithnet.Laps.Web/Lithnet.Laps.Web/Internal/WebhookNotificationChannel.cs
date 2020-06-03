using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Text;
using Lithnet.Laps.Web.App_LocalResources;
using Lithnet.Laps.Web.AppSettings;
using NLog;

namespace Lithnet.Laps.Web.Internal
{
    public class WebhookNotificationChannel : INotificationChannel
    {
        private readonly ILogger logger;

        private readonly ITemplates templates;

        private readonly GlobalAuditSettings globalAuditSettings;

        public string Name => "webhook";

        public WebhookNotificationChannel(ILogger logger, ITemplates templates, GlobalAuditSettings globalAuditSettings)
        {
            this.logger = logger;
            this.templates = templates;
            this.globalAuditSettings = globalAuditSettings;
        }

        public void ProcessNotification(AuditableAction action, Dictionary<string, string> tokens)
        {
            HttpClient client = new HttpClient();
            HttpRequestMessage message = new HttpRequestMessage();

            string content;

            if (action.IsSuccess)
            {
                content = templates.SlackSuccessTemplate;
            }
            else
            {
                content = templates.SlackFailureTemplate;
            }

            content = TokenReplacer.ReplaceAsJson(tokens, content);

            message.Content = new StringContent(content, Encoding.UTF8, "application/json");
            message.RequestUri = new Uri("");
            message.Method = HttpMethod.Post;
            client.SendAsync(message).GetAwaiter().GetResult();
        }
    }
}