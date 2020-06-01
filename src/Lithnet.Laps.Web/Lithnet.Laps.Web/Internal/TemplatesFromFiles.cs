using System;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using NLog;

namespace Lithnet.Laps.Web.Internal
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

        private readonly IWebHostEnvironment env;

        public TemplatesFromFiles(ILogger logger, IWebHostEnvironment env)
        {
            this.env = env;
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
                string text = File.ReadAllText(Path.Combine(this.env.ContentRootPath, $"App_Data\\Templates\\{templateName}"));

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