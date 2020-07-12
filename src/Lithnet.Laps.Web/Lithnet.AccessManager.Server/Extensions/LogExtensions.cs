using System;
using NLog;

namespace Lithnet.AccessManager.Server.Extensions
{
    public static class LogExtensions
    {
        public static void LogEventError(this ILogger logger, int eventId, string logMessage)
        {
            LogEventError(logger, eventId, logMessage, null);
        }

        public static void LogEventError(this ILogger logger, int eventId, string message, Exception ex)
        {
            LogEvent(logger, eventId, LogLevel.Error, message, ex);
        }

        public static void LogEventSuccess(this ILogger logger, int eventId, string message)
        {
            LogEvent(logger, eventId, LogLevel.Info, message, null);
        }

        public static void LogEvent(this ILogger logger, int eventId, LogLevel logLevel, string message, Exception ex)
        {
            LogEventInfo logEvent = new LogEventInfo(logLevel, logger.Name, message);
            logEvent.Properties.Add("EventID", eventId);
            logEvent.Exception = ex;

            logger.Info(logEvent);
        }
    }
}