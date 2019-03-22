using System;
using NLog;

namespace Lithnet.Laps.Web.Audit
{
    public class TemplatesFromFiles: ITemplates
    {
        private static string _logSuccessTemplate = null;
        private static string _logFailureTemplate = null;
        private static string _emailSuccessTemplate = null;
        private static string _emailFailureTemplate = null;

        private readonly ILogger logger;

        public TemplatesFromFiles(ILogger logger)
        {
            this.logger = logger;
            MakeSureTemplatesAreLoaded();
        }

        private void MakeSureTemplatesAreLoaded()
        {
            if (_logSuccessTemplate == null) _logSuccessTemplate = LoadTemplate("LogAuditSuccess.txt");
            if (_logFailureTemplate == null) _logFailureTemplate = LoadTemplate("LogAuditFailure.txt");
            if (_emailSuccessTemplate == null) _emailSuccessTemplate = LoadTemplate("EmailAuditSuccess.html");
            if (_emailFailureTemplate == null) _emailFailureTemplate = LoadTemplate("EmailAuditFailure.html");
        }

        private string LoadTemplate(string templateName)
        {
            try
            {
                string text = System.IO.File.ReadAllText(System.Web.HttpContext.Current.Server.MapPath($"~\\App_Data\\Templates\\{templateName}"));

                return string.IsNullOrWhiteSpace(text) ? null : text;
            }
            catch (Exception ex)
            {
                LogErrorEvent(EventIDs.ErrorLoadingTemplateResource, $"Could not load template {templateName}", ex);
                return null;
            }
        }

        public void LogErrorEvent(int eventID, string logMessage, Exception ex)
        {
            // FIXME: this is the same code as Reporting.LogErrorEvent.
            // We should create a service for logging, with only LogErrorEvent and LogSuccessEvent.
            LogEventInfo logEvent = new LogEventInfo(LogLevel.Error, logger.Name, logMessage);
            logEvent.Properties.Add("EventID", eventID);
            logEvent.Exception = ex;

            logger.Log(logEvent);
        }

        public string LogSuccessTemplate => _logSuccessTemplate;
        public string LogFailureTemplate => _logFailureTemplate;
        public string EmailSuccessTemplate => _emailSuccessTemplate;
        public string EmailFailureTemplate => _emailFailureTemplate;
    }
}