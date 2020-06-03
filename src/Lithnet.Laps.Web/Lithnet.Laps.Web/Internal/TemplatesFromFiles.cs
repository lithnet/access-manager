using System;
using System.IO;
using Lithnet.Laps.Web.App_LocalResources;
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

        private static string slackFailureTemplate = null;

        private static string slackSuccessTemplate = null;

        public string LogSuccessTemplate => TemplatesFromFiles.logSuccessTemplate;

        public string LogFailureTemplate => TemplatesFromFiles.logFailureTemplate;

        public string EmailSuccessTemplate => TemplatesFromFiles.emailSuccessTemplate;

        public string EmailFailureTemplate => TemplatesFromFiles.emailFailureTemplate;

        public string SlackSuccessTemplate => TemplatesFromFiles.slackSuccessTemplate;

        public string SlackFailureTemplate => TemplatesFromFiles.slackFailureTemplate;

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

            if (slackSuccessTemplate == null)
            {
                slackSuccessTemplate = this.LoadTemplate("SlackTemplateSuccess.json");
            }

            if (slackFailureTemplate == null)
            {
                slackFailureTemplate = this.LoadTemplate("SlackTemplateFailure.json");
            }

            if (TemplatesFromFiles.emailSuccessTemplate == null)
            {
                TemplatesFromFiles.emailSuccessTemplate = this.LoadTemplate("EmailAuditSuccess.html") ?? $"<html><head/><body><pre>{LogMessages.DefaultAuditSuccessText}</pre></body></html>";
            }

            if (TemplatesFromFiles.emailFailureTemplate == null)
            {
                TemplatesFromFiles.emailFailureTemplate = this.LoadTemplate("EmailAuditFailure.html") ?? $"<html><head/><body><pre>{LogMessages.DefaultAuditFailureText}</pre></body></html>";
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