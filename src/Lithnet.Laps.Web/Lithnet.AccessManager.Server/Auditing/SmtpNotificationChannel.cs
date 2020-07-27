using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Channels;
using HtmlAgilityPack;
using Lithnet.AccessManager.Server.App_LocalResources;
using Lithnet.AccessManager.Server.Configuration;
using Lithnet.AccessManager.Server.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lithnet.AccessManager.Server.Auditing
{
    public class SmtpNotificationChannel : NotificationChannel<SmtpNotificationChannelDefinition>
    {
        private readonly ILogger logger;

        private readonly EmailOptions emailSettings;

        private readonly ITemplateProvider templates;

        private readonly RandomNumberGenerator rng;

        public override string Name => "smtp";

        protected override IList<SmtpNotificationChannelDefinition> NotificationChannelDefinitions { get; }

        public SmtpNotificationChannel(ILogger<SmtpNotificationChannel> logger, IOptions<EmailOptions> emailSettings, ITemplateProvider templates, IOptions<AuditOptions> auditSettings, ChannelWriter<Action> queue, RandomNumberGenerator rng)
            : base(logger, queue)
        {
            this.logger = logger;
            this.rng = rng;
            this.emailSettings = emailSettings.Value;
            this.templates = templates;
            this.NotificationChannelDefinitions = auditSettings.Value.NotificationChannels.Smtp;
        }

        protected override void Send(AuditableAction action, Dictionary<string, string> tokens, SmtpNotificationChannelDefinition settings)
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
                this.logger.LogTrace("SMTP is not configured, discarding mail message");
                return;
            }

            using SmtpClient client = new SmtpClient()
            {
                Host = this.emailSettings.Host,
                Port = this.emailSettings.Port,
                UseDefaultCredentials = this.emailSettings.UseDefaultCredentials,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                EnableSsl = this.emailSettings.UseSsl,
                Credentials = this.GetCredentials()
            };

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
                this.logger.LogTrace($"Not sending notification email because there are no recipients");
                return;
            }

            message.IsBodyHtml = true;
            message.Subject = subject;
            message.Body = body;
            client.Send(message);
        }

        private NetworkCredential GetCredentials()
        {

            if (!this.emailSettings.UseDefaultCredentials && !string.IsNullOrWhiteSpace(this.emailSettings.Username))
            {
                return null;
            }

            return new NetworkCredential(this.emailSettings.Username, this.emailSettings.Password?.GetSecret());
        }
    }
}