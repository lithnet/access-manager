using System;
using NLog;

namespace Lithnet.Laps.Web.Internal
{
    public interface IReporting
    {
        void GenerateAuditEvent(AuditableAction action);
        void LogEvent(int eventId, LogLevel logLevel, string message, Exception ex);
        void LogEventError(int eventId, string logMessage);
        void LogEventError(int eventId, string message, Exception ex);
        void LogEventSuccess(int eventId, string message);
    }
}