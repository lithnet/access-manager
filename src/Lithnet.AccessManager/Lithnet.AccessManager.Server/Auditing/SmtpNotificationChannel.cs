using System;
using System.Collections.Generic;
using System.Threading.Channels;
using HtmlAgilityPack;
using Lithnet.AccessManager.Server.App_LocalResources;
using Lithnet.AccessManager.Server.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lithnet.AccessManager.Server.Auditing
{
    public class SmtpNotificationChannel : NotificationChannel<SmtpNotificationChannelDefinition>
    {
        private readonly ILogger logger;
        private readonly ITemplateProvider templates;
        private readonly ISmtpProvider smtpProvider;

        public override string Name => "smtp";

        protected override IList<SmtpNotificationChannelDefinition> NotificationChannelDefinitions { get; }

        public SmtpNotificationChannel(ILogger<SmtpNotificationChannel> logger, ITemplateProvider templates, IOptionsSnapshot<AuditOptions> auditSettings, ChannelWriter<Action> queue, ISmtpProvider smtpProvider)
            : base(logger, queue)
        {
            this.logger = logger;
            this.templates = templates;
            this.NotificationChannelDefinitions = auditSettings.Value.NotificationChannels.Smtp;
            this.smtpProvider = smtpProvider;
        }

        protected override void Send(AuditableAction action, Dictionary<string, string> tokens, SmtpNotificationChannelDefinition settings)
        {
            string body = action.IsSuccess ? templates.GetTemplate(settings.TemplateSuccess) : templates.GetTemplate(settings.TemplateFailure);
            string subject = GetSubjectLine(body) ?? (action.IsSuccess ? LogMessages.AuditEmailSubjectSuccess : LogMessages.AuditEmailSubjectFailure);

            body = TokenReplacer.ReplaceAsHtml(tokens, body);
            subject = TokenReplacer.ReplaceAsPlainText(tokens, subject);

            HashSet<string> recipients = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            settings.EmailAddresses.ForEach(t => recipients.Add(t));

            if (recipients.Remove("{user.EmailAddress}"))
            {
                if (action?.User?.EmailAddress != null)
                {
                    recipients.Add(action.User.EmailAddress);
                }
            }

            this.smtpProvider.SendEmail(recipients, subject, body);
        }

        private string GetSubjectLine(string content)
        {
            HtmlDocument d = new HtmlDocument();
            d.LoadHtml(content);

            var titleNode = d.DocumentNode.SelectSingleNode("html/head/title");

            return string.IsNullOrWhiteSpace(titleNode?.InnerText) ? null : titleNode.InnerText;
        }
    }
}