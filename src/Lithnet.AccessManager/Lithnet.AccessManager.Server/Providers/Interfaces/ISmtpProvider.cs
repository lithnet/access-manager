using System.Collections.Generic;

namespace Lithnet.AccessManager.Server
{
    public interface ISmtpProvider
    {
        bool IsConfigured { get; }

        void SendEmail(IEnumerable<string> recipients, string subject, string body);

        void SendEmail(string recipients, string subject, string body);
    }
}