using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Web;
using Lithnet.Laps.Web.App_LocalResources;
using Lithnet.Laps.Web.Mail;
using Lithnet.Laps.Web.Models;
using Lithnet.Laps.Web.Security.Authorization;
using NLog;

namespace Lithnet.Laps.Web.Audit
{
    public sealed class Reporting: IReporting
    {
        private readonly ILogger logger;
        private readonly ILapsConfig configSection;
        private readonly IMailer mailer;
        private readonly ITemplates templates;

        public Reporting(ILogger logger, ILapsConfig configSection, IMailer mailer, ITemplates templates)
        {
            this.logger = logger;
            this.configSection = configSection;
            this.mailer = mailer;
            this.templates = templates;
        }

        public void LogErrorEvent(int eventID, string logMessage, Exception ex)
        {
            LogEventInfo logEvent = new LogEventInfo(LogLevel.Error, logger.Name, logMessage);
            logEvent.Properties.Add("EventID", eventID);
            logEvent.Exception = ex;

            logger.Log(logEvent);
        }

        public void LogSuccessEvent(int eventID, string logMessage)
        {
            LogEventInfo logEvent = new LogEventInfo(LogLevel.Error, logger.Name, logMessage);
            logEvent.Properties.Add("EventID", eventID);

            logger.Info(logEvent);
        }

        public void PerformAuditSuccessActions(LapRequestModel model, ITarget target, AuthorizationResponse authorizationResponse, IUser user, IComputer computer, Password password)
        {
            Dictionary<string, string> tokens = BuildTokenDictionary(target, authorizationResponse, user, computer, password, model.ComputerName);
            string logSuccessMessage = templates.LogSuccessTemplate ?? LogMessages.DefaultAuditSuccessText;
            string emailSuccessMessage = templates.EmailSuccessTemplate ?? $"<html><head/><body><pre>{LogMessages.DefaultAuditSuccessText}</pre></body></html>";

            LogEventInfo logEvent = new LogEventInfo(LogLevel.Info, logger.Name, ReplaceTokens(tokens, logSuccessMessage, false));
            logEvent.Properties.Add("EventID", EventIDs.PasswordAccessed);
            logger.Log(logEvent);

            try
            {
                var recipients = BuildRecipientList(target, authorizationResponse, true, user);

                if (recipients.Count > 0)
                {
                    string subject = ReplaceTokens(tokens, LogMessages.AuditEmailSubjectSuccess, false);
                    mailer.SendEmail(recipients, subject, ReplaceTokens(tokens, emailSuccessMessage, true));
                }
            }
            catch (Exception iex)
            {
                LogErrorEvent(EventIDs.AuditErrorCannotSendSuccessEmail, "An error occurred sending the success audit email", iex);
            }
        }

        public void PerformAuditFailureActions(LapRequestModel model, string userMessage, int eventID, string logMessage, Exception ex, ITarget target, AuthorizationResponse authorizationResponse, IUser user, IComputer computer)
        {
            Dictionary<string, string> tokens = BuildTokenDictionary(target, authorizationResponse, user, computer, null, model.ComputerName, logMessage ?? userMessage);
            string logFailureMessage = templates.LogFailureTemplate ?? LogMessages.DefaultAuditFailureText;
            string emailFailureMessage = templates.EmailFailureTemplate ?? $"<html><head/><body><pre>{LogMessages.DefaultAuditFailureText}</pre></body></html>";

            LogErrorEvent(eventID, ReplaceTokens(tokens, logFailureMessage, false), ex);

            try
            {
                var recipients = BuildRecipientList(target, authorizationResponse, false);

                if (recipients.Count > 0)
                {
                    string subject = ReplaceTokens(tokens, LogMessages.AuditEmailSubjectFailure, false);
                    mailer.SendEmail(recipients, subject, ReplaceTokens(tokens, emailFailureMessage, true));
                }
            }
            catch (Exception iex)
            {
                LogErrorEvent(EventIDs.AuditErrorCannotSendFailureEmail, "An error occurred sending the failure audit email", iex);
            }
        }

        private Dictionary<string, string> BuildTokenDictionary(ITarget target = null, AuthorizationResponse authorizationResponse = null, IUser user = null, IComputer computer = null, Password password = null, string requestedComputerName = null, string detailMessage = null)
        {
            Dictionary<string, string> pairs = new Dictionary<string, string> {
                { "{user.SamAccountName}", user?.SamAccountName},
                { "{user.DisplayName}", user?.DisplayName},
                { "{user.UserPrincipalName}", user?.UserPrincipalName},
                { "{user.Sid}", user?.Sid?.ToString()},
                { "{user.DistinguishedName}", user?.DistinguishedName},
                { "{user.Description}", user?.Description},
                { "{user.EmailAddress}", user?.EmailAddress},
                { "{user.Guid}", user?.Guid?.ToString()},
                { "{user.GivenName}", user?.GivenName},
                { "{user.Surname}", user?.Surname},
                { "{computer.SamAccountName}", computer?.SamAccountName},
                { "{computer.DistinguishedName}", computer?.DistinguishedName},
                { "{computer.Description}", computer?.Description},
                { "{computer.DisplayName}", computer?.DisplayName},
                { "{computer.Guid}", computer?.Guid?.ToString()},
                { "{computer.Sid}", computer?.Sid?.ToString()},
                { "{requestedComputerName}", requestedComputerName},
                // FIXME: The token {reader.Principal} actually contains the authorization details.
                // This is the principal when the ConfigurationFileAuthorizationService is used, but it can be something
                // else in case of other authorization services.
                { "{reader.Principal}", authorizationResponse?.ExtraInfo},
                { "{reader.Notify}", string.Join(",", authorizationResponse?.UsersToNotify?.All ?? ImmutableHashSet<string>.Empty)},
                { "{target.Notify}", string.Join(",", target?.UsersToNotify?.All ?? ImmutableHashSet<string>.Empty)},
                { "{target.ID}", target?.TargetName},
                { "{target.IDType}", target?.TargetType.ToString()},
                { "{message}", detailMessage},
                { "{request.IPAddress}", HttpContext.Current?.Request?.UserHostAddress},
                { "{request.HostName}", HttpContext.Current?.Request?.UserHostName},
                { "{request.Xff}", HttpContext.Current?.Request?.GetXffIP()},
                { "{request.XffAll}", HttpContext.Current?.Request?.GetXffList()},
                { "{request.UnmaskedIPAddress}", HttpContext.Current?.Request?.GetUnmaskedIP()},
                { "{datetime}", DateTime.Now.ToString(CultureInfo.CurrentCulture)},
                { "{datetimeutc}", DateTime.UtcNow.ToString(CultureInfo.CurrentCulture)},
                { "{computer.LapsExpiryDate}", password?.ExpirationTime?.ToString(CultureInfo.CurrentCulture)},
            };

            return pairs;
        }

        private string ReplaceTokens(Dictionary<string, string> tokens, string text, bool isHtml)
        {
            foreach (KeyValuePair<string, string> token in tokens)
            {
                text = text.Replace(token.Key, isHtml ? HttpUtility.HtmlEncode(token.Value) : token.Value);
            }

            return text;
        }

        private IImmutableSet<string> BuildRecipientList(ITarget target, AuthorizationResponse authorizationResponse, bool success, IUser user = null)
        {
            var usersToNotify = target?.UsersToNotify ?? new UsersToNotify();

            if (authorizationResponse != null)
            {
                usersToNotify = usersToNotify.Union(authorizationResponse.UsersToNotify);
            }

            if (configSection.UsersToNotify != null)
            {
                usersToNotify = usersToNotify.Union(configSection.UsersToNotify);
            }

            if (!string.IsNullOrWhiteSpace(user?.EmailAddress))
            {
                // FIXME: This seems to be an undocumented feature?
                usersToNotify = usersToNotify.WithUserReplaced("{user.EmailAddress}", user?.EmailAddress);
            }

            return success ? usersToNotify.OnSuccess : usersToNotify.OnFailure;
        }


    }
}