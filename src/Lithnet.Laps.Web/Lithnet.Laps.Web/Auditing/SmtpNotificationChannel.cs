using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Net;
using System.Net.Mail;
using System.Threading.Channels;
using System.Xml;
using HtmlAgilityPack;
using Lithnet.Laps.Web.App_LocalResources;
using Lithnet.Laps.Web.AppSettings;
using NLog;

namespace Lithnet.Laps.Web.Internal
{
    public class SmtpNotificationChannel : NotificationChannel<ISmtpChannelSettings>
    {
        private readonly ILogger logger;

        private readonly IEmailSettings emailSettings;

        private readonly ITemplateProvider templates;

        private readonly IAuditSettings auditSettings;

        public override string Name => "smtp";

        public SmtpNotificationChannel(ILogger logger, IEmailSettings emailSettings, ITemplateProvider templates, IAuditSettings auditSettings, ChannelWriter<Action> queue)
            : base(logger, queue)
        {
            this.logger = logger;
            this.emailSettings = emailSettings;
            this.templates = templates;
            this.auditSettings = auditSettings;
        }

        public override void ProcessNotification(AuditableAction action, Dictionary<string, string> tokens, IImmutableSet<string> notificationChannels)
        {
            this.ProcessNotification(action, tokens, notificationChannels, this.auditSettings.Channels.Smtp);
        }

        protected override void Send(AuditableAction action, Dictionary<string, string> tokens, ISmtpChannelSettings settings)
        {
            string message = action.IsSuccess ? templates.GetTemplate(settings.TemplateSuccess) : templates.GetTemplate(settings.TemplateFailure);
            string subject = GetSubjectLine(message, action.IsSuccess);

            message = TokenReplacer.ReplaceAsHtml(tokens, message);
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

            this.SendEmail(recipients, subject, message);
        }

        private string GetSubjectLine(string content, bool isSuccess)
        {
            HtmlDocument d = new HtmlDocument();
            d.LoadHtml(content);

            var titleNode = d.DocumentNode.SelectSingleNode("html/head/title");

            if (titleNode == null || string.IsNullOrWhiteSpace(titleNode.InnerText))
            {
                return isSuccess ? LogMessages.AuditEmailSubjectSuccess : LogMessages.AuditEmailSubjectFailure;
            }

            return titleNode.InnerText;
        }

        private void SendEmail(IEnumerable<string> recipients, string subject, string body)
        {
            if (!this.emailSettings.IsConfigured)
            {
                this.logger.Trace("SMTP is not configured, discarding mail message");
                return;
            }

            using SmtpClient client = new SmtpClient(this.emailSettings.Host, this.emailSettings.Port)
            {
                UseDefaultCredentials = this.emailSettings.UseDefaultCredentials,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                EnableSsl = this.emailSettings.UseSsl
            };

            if (!this.emailSettings.UseDefaultCredentials && !string.IsNullOrWhiteSpace(this.emailSettings.Username))
            {
                client.Credentials = new NetworkCredential(this.emailSettings.Username, this.emailSettings.Password);
            }

            using MailMessage message = new MailMessage
            {
                From = new MailAddress(this.emailSettings.FromAddress)
            };

            foreach (string recipient in recipients)
            {
                message.To.Add(recipient);
            }

            if (message.To.Count == 0)
            {
                this.logger.Trace($"Not sending notification email because there are no recipients");
                return;
            }

            message.IsBodyHtml = true;
            message.Subject = subject;
            message.Body = body;
            client.Send(message);
        }
    }
}