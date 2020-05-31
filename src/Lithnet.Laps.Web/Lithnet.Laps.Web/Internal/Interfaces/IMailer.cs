using System.Collections.Generic;

namespace Lithnet.Laps.Web.Internal
{
    public interface IMailer
    {
        void SendEmail(IEnumerable<string> recipients, string subject, string body);
    }
}