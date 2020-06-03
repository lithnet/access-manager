using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Net;
using System.Net.Mail;
using Lithnet.Laps.Web.App_LocalResources;
using Lithnet.Laps.Web.AppSettings;
using NLog;

namespace Lithnet.Laps.Web.Internal
{
    public class SmtpNotificationChannel : INotificationChannel
    {
        private readonly ILogger logger;

        private readonly IEmailSettings emailSettings;

        private readonly ITemplates templates;

        private readonly GlobalAuditSettings globalAuditSettings;

        public string Name => "smtp";

        public SmtpNotificationChannel(ILogger logger, IEmailSettings emailSettings, ITemplates templates, GlobalAuditSettings globalAuditSettings)
        {
            this.logger = logger;
            this.emailSettings = emailSettings;
            this.templates = templates;
            this.globalAuditSettings = globalAuditSettings;
        }

        public void ProcessNotification(AuditableAction action, Dictionary<string, string> tokens)
        {
            IImmutableSet<string> recipients = this.BuildRecipientList(action);

            if (recipients.Count <= 0)
            {
                return;
            }

            string message;
            string subject;

            if (action.IsSuccess)
            {
                message = this.templates.EmailSuccessTemplate;
                subject = LogMessages.AuditEmailSubjectSuccess;
            }
            else
            {
                message = this.templates.EmailFailureTemplate;
                subject = LogMessages.AuditEmailSubjectFailure;
            }

            message = TokenReplacer.ReplaceAsHtml(tokens, message);
            subject = TokenReplacer.ReplaceAsPlainText(tokens, subject);

            this.SendEmail(recipients, subject, message);
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

        private IImmutableSet<string> BuildRecipientList(AuditableAction action)
        {
            HashSet<string> usersToNotify = new HashSet<string>();

            if (action.AuthzResponse != null)
            {
                action.AuthzResponse.NotificationRecipients?.ForEach(t => usersToNotify.Add(t));
            }

            if (action.IsSuccess)
            {
                this.globalAuditSettings.SuccessRecipients?.ForEach(t => usersToNotify.Add(t));
            }
            else
            {
                this.globalAuditSettings.FailureRecipients?.ForEach(t => usersToNotify.Add(t));
            }

            if (!string.IsNullOrWhiteSpace(action.User?.EmailAddress))
            {
                if (usersToNotify.Remove("{user.EmailAddress}"))
                {
                    usersToNotify.Add(action.User.EmailAddress);
                }
            }

            return usersToNotify.ToImmutableHashSet();
        }

    }
}