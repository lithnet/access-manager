using System.Collections.Generic;
using System.Net.Mail;
using NLog;

namespace Lithnet.Laps.Web.Mail
{
    public class SmtpMailer : IMailer
    {
        private readonly ILogger logger;

        public SmtpMailer(ILogger logger)
        {
            this.logger = logger;
        }

        private bool IsSmtpConfigured()
        {
            return !string.IsNullOrWhiteSpace(new SmtpClient().Host);
        }

        public void SendEmail(IEnumerable<string> recipients, string subject, string body)
        {
            if (!IsSmtpConfigured())
            {
                logger.Trace("SMTP is not configured, discarding mail message");
                return;
            }

            using (SmtpClient client = new SmtpClient())
            {
                using (MailMessage message = new MailMessage())
                {
                    foreach (string recipient in recipients)
                    {
                        message.To.Add(recipient);
                    }

                    if (message.To.Count == 0)
                    {
                        logger.Trace($"Not sending notification email because there are no recipients");
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