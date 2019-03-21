using System;
using Lithnet.Laps.Web.Security.Authorization.ConfigurationFile;
using Lithnet.Laps.Web.Models;
using Lithnet.Laps.Web.Security.Authorization;

namespace Lithnet.Laps.Web.Audit
{
    public interface IReporting
    {
        void LogSuccessEvent(int eventID, string logMessage);
        void LogErrorEvent(int eventID, string logMessage, Exception ex);
        void PerformAuditSuccessActions(LapRequestModel model, ITarget target, AuthorizationResponse authorizationResponse, IUser user, IComputer computer, Password password);
        void PerformAuditFailureActions(LapRequestModel model, string userMessage, int eventID, string logMessage, Exception ex, ITarget target, AuthorizationResponse authorizationResponse, IUser user, IComputer computer);
    }
}
