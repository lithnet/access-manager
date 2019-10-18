using System;
using NLog;

namespace Lithnet.Laps.Web.Audit
{
    public class TemplatesFromFiles: ITemplates
    {
        private static string logSuccessTemplate = null;

        private static string logFailureTemplate = null;

        private static string emailSuccessTemplate = null;

        private static string emailFailureTemplate = null;

        public string LogSuccessTemplate => TemplatesFromFiles.logSuccessTemplate;

        public string LogFailureTemplate => TemplatesFromFiles.logFailureTemplate;

        public string EmailSuccessTemplate => TemplatesFromFiles.emailSuccessTemplate;

        public string EmailFailureTemplate => TemplatesFromFiles.emailFailureTemplate;

        private readonly ILogger logger;

        public TemplatesFromFiles(ILogger logger)
        {
            this.logger = logger;
            this.MakeSureTemplatesAreLoaded();
        }

        private void MakeSureTemplatesAreLoaded()
        {
            if (TemplatesFromFiles.logSuccessTemplate == null)
            {
                TemplatesFromFiles.logSuccessTemplate = this.LoadTemplate("LogAuditSuccess.txt");
            }

            if (TemplatesFromFiles.logFailureTemplate == null)
            {
                TemplatesFromFiles.logFailureTemplate = this.LoadTemplate("LogAuditFailure.txt");
            }

            if (TemplatesFromFiles.emailSuccessTemplate == null)
            {
                TemplatesFromFiles.emailSuccessTemplate = this.LoadTemplate("EmailAuditSuccess.html");
            }

            if (TemplatesFromFiles.emailFailureTemplate == null)
            {
                TemplatesFromFiles.emailFailureTemplate = this.LoadTemplate("EmailAuditFailure.html");
            }
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
                this.LogErrorEvent(EventIDs.ErrorLoadingTemplateResource, $"Could not load template {templateName}", ex);
                return null;
            }
        }

        public void LogErrorEvent(int eventID, string logMessage, Exception ex)
        {
            // FIXME: this is the same code as Reporting.LogErrorEvent.
            // We should create a service for logging, with only LogErrorEvent and LogSuccessEvent.
            LogEventInfo logEvent = new LogEventInfo(LogLevel.Error, this.logger.Name, logMessage);
            logEvent.Properties.Add("EventID", eventID);
            logEvent.Exception = ex;

            this.logger.Log(logEvent);
        }
    }
}