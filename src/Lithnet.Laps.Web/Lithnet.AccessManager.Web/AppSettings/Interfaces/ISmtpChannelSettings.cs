using System.Collections.Generic;

namespace Lithnet.AccessManager.Web.AppSettings
{
    public interface ISmtpChannelSettings : IChannelSettings
    {
        IEnumerable<string> EmailAddresses { get; }
        
        string TemplateFailure { get; }

        string TemplateSuccess { get; }
    }
}