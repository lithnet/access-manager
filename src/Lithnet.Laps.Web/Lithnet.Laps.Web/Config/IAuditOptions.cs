using System.Collections.Generic;
using Lithnet.Laps.Web.Audit;

namespace Lithnet.Laps.Web.Config
{
    public interface IAuditOptions
    {
        bool NotifySuccess { get; }

        bool NotifyFailure { get; }

        AuditReasonFieldState UserSuppliedReason { get; }

        IList<string> EmailAddresses { get; }

        UsersToNotify UsersToNotify { get; }
    }
}