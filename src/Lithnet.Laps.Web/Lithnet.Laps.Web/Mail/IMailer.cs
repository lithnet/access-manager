using System.Collections.Generic;

namespace Lithnet.Laps.Web.Mail
{
    public interface IMailer
    {
        void SendEmail(IEnumerable<string> recipients, string subject, string body);
    }
}