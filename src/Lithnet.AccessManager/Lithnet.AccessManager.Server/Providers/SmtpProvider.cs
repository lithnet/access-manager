using Lithnet.AccessManager.Server.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;

namespace Lithnet.AccessManager.Server
{
    public class SmtpProvider : ISmtpProvider
    {
        private readonly IOptionsMonitor<EmailOptions> emailOptions;
        private readonly ILogger<SmtpProvider> logger;
        private readonly IProtectedSecretProvider secretProvider;

        public SmtpProvider(IOptionsMonitor<EmailOptions> emailOptions, ILogger<SmtpProvider> logger, IProtectedSecretProvider secretProvider)
        {
            this.emailOptions = emailOptions;
            this.logger = logger;
            this.secretProvider = secretProvider;
        }

        public bool IsConfigured => this.emailOptions.CurrentValue.IsConfigured;

        public void SendEmail(string recipients, string subject, string body)
        {
            if (string.IsNullOrWhiteSpace(recipients))
            {
                this.logger.LogTrace($"Not sending notification email because there are no recipients");
                return;
            }

            this.SendEmail(recipients.Split(';', ','), subject, body);
        }

        public void SendEmail(IEnumerable<string> recipients, string subject, string body)
        {
            EmailOptions emailSettings = this.emailOptions.CurrentValue;

            if (!emailSettings.IsConfigured)
            {
                this.logger.LogTrace("SMTP is not configured, discarding mail message");
                return;
            }

            using SmtpClient client = new SmtpClient()
            {
                Host = emailSettings.Host,
                Port = emailSettings.Port,
                UseDefaultCredentials = emailSettings.UseDefaultCredentials,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                EnableSsl = emailSettings.UseSsl,
                Credentials = this.GetCredentials(emailSettings)
            };

            using MailMessage message = new MailMessage
            {
                From = new MailAddress(emailSettings.FromAddress)
            };

            foreach (string recipient in new HashSet<string>(recipients, StringComparer.OrdinalIgnoreCase))
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

        private NetworkCredential GetCredentials(EmailOptions emailSettings)
        {
            if (emailSettings.UseDefaultCredentials || string.IsNullOrWhiteSpace(emailSettings.Username))
            {
                return null;
            }

            return new NetworkCredential(emailSettings.Username, this.secretProvider.UnprotectSecret(emailSettings.Password));
        }
    }
}
