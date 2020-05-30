using System;
using Lithnet.Laps.Web.Config;
using Lithnet.Laps.Web.JsonTargets;
using Lithnet.Laps.Web.Models;

namespace Lithnet.Laps.Web.Audit
{
    public interface IReporting
    {
        void LogSuccessEvent(int eventID, string logMessage);

        void LogErrorEvent(int eventID, string logMessage, Exception ex);

        void PerformAuditSuccessActions(LapRequestModel model, AuthorizationResponse authorizationResponse, IUser user, IComputer computer, PasswordData passwordData);

        void PerformAuditFailureActions(LapRequestModel model, string userMessage, int eventID, string logMessage, Exception ex, AuthorizationResponse authorizationResponse, IUser user, IComputer computer);
    }
}
