using System.Collections.Generic;

namespace Lithnet.Laps.Web.Authorization
{
    public interface IAuditNotificationChannels
    {
        IList<string> OnFailure { get; set; }

        IList<string> OnSuccess { get; set; }
    }
}