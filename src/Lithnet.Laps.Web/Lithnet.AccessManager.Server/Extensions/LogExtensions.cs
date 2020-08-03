using System;
using Microsoft.Extensions.Logging;

namespace Lithnet.AccessManager.Server.Extensions
{
    public static class LogExtensions
    {
        public static void LogEventWarning(this ILogger logger, int eventId, string logMessage)
        {
            LogEventWarning(logger, eventId, logMessage, null);
        }

        public static void LogEventWarning(this ILogger logger, int eventId, string message, Exception ex)
        {
            LogEvent(logger, eventId, LogLevel.Warning, message, ex);
        }

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
            LogEvent(logger, eventId, LogLevel.Information, message, null);
        }

        public static void LogEvent(this ILogger logger, int eventId, LogLevel logLevel, string message, Exception ex)
        {
            logger.Log(logLevel, eventId, ex, message);
        }
    }
}