using System.Collections.Generic;

namespace Lithnet.AccessManager.Web.AppSettings
{
    public interface IAuditSettings
    {
        NotificationChannels Channels { get; }

        IEnumerable<string> FailureChannels { get; }

        IEnumerable<string> SuccessChannels { get; }
    }
}