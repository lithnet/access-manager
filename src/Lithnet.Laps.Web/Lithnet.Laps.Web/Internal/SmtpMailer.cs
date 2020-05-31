using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using Lithnet.Laps.Web.AppSettings;
using NLog;

namespace Lithnet.Laps.Web.Internal
{
    public class SmtpMailer : IMailer
    {
        private readonly ILogger logger;

        private readonly IEmailSettings emailSettings;

        public SmtpMailer(ILogger logger, IEmailSettings emailSettings)
        {
            this.logger = logger;
            this.emailSettings = emailSettings;
        }

        public void SendEmail(IEnumerable<string> recipients, string subject, string body)
        {
            if (!this.emailSettings.IsConfigured)
            {
                this.logger.Trace("SMTP is not configured, discarding mail message");
                return;
            }

            using (SmtpClient client = new SmtpClient(this.emailSettings.Host, this.emailSettings.Port))
            {
                client.UseDefaultCredentials = this.emailSettings.UseDefaultCredentials;
                client.DeliveryMethod = SmtpDeliveryMethod.Network;
                client.EnableSsl = this.emailSettings.UseSsl;
                
                if (!this.emailSettings.UseDefaultCredentials && !string.IsNullOrWhiteSpace(this.emailSettings.Username))
                {
                    client.Credentials = new NetworkCredential(this.emailSettings.Username, this.emailSettings.Password);
                }
                
                using (MailMessage message = new MailMessage())
                {
                    message.From = new MailAddress(this.emailSettings.FromAddress);

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
    }
}