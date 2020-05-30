using System.Collections.Generic;
using Lithnet.Laps.Web.Audit;

namespace Lithnet.Laps.Web.Config
{
    public interface IAuditSettings
    {
        bool NotifySuccess { get; }

        bool NotifyFailure { get; }

        IEnumerable<string> EmailAddresses { get; }

        UsersToNotify UsersToNotify { get; }
    }
}