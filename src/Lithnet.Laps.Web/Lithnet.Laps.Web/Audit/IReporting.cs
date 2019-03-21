using System;
using System.DirectoryServices.AccountManagement;
using Lithnet.Laps.Web.Authorization;
using Lithnet.Laps.Web.Models;

namespace Lithnet.Laps.Web.Audit
{
    public interface IReporting
    {
        void LogSuccessEvent(int eventID, string logMessage);
        void LogErrorEvent(int eventID, string logMessage, Exception ex);
        void PerformAuditSuccessActions(LapRequestModel model, ITarget target, AuthorizationResponse authorizationResponse, UserPrincipal user, IComputer computer, Password password);
        void PerformAuditFailureActions(LapRequestModel model, string userMessage, int eventID, string logMessage, Exception ex, ITarget target, AuthorizationResponse authorizationResponse, UserPrincipal user, IComputer computer);
    }
}
