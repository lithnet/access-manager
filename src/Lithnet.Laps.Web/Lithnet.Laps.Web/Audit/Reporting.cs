using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Globalization;
using System.Net.Mail;
using System.Web;
using Lithnet.Laps.Web.App_LocalResources;
using Lithnet.Laps.Web.Authorization;
using Lithnet.Laps.Web.Models;
using NLog;

namespace Lithnet.Laps.Web.Audit
{
    internal static class Reporting
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static readonly string LogSuccessTemplate = LoadTemplate("LogAuditSuccess.txt");
        private static readonly string LogFailureTemplate = LoadTemplate("LogAuditFailure.txt");
        private static readonly string EmailSuccessTemplate = LoadTemplate("EmailAuditSuccess.html");
        private static readonly string EmailFailureTemplate = LoadTemplate("EmailAuditFailure.html");

        public static void LogErrorEvent(int eventID, string logMessage, Exception ex)
        {
            LogEventInfo logEvent = new LogEventInfo(LogLevel.Error, Reporting.Logger.Name, logMessage);
            logEvent.Properties.Add("EventID", eventID);
            logEvent.Exception = ex;

            Reporting.Logger.Log(logEvent);
        }

        public static void LogSuccessEvent(int eventID, string logMessage)
        {
            LogEventInfo logEvent = new LogEventInfo(LogLevel.Error, Reporting.Logger.Name, logMessage);
            logEvent.Properties.Add("EventID", eventID);

            Reporting.Logger.Info(logEvent);
        }

        public static void PerformAuditSuccessActions(LapRequestModel model, TargetElement target, AuthorizationResponse authorizationResponse, UserPrincipal user, ComputerPrincipal computer, SearchResult searchResult)
        {
            Dictionary<string, string> tokens = BuildTokenDictionary(target, authorizationResponse, user, computer, searchResult, model.ComputerName);
            string logSuccessMessage = Reporting.LogSuccessTemplate ?? LogMessages.DefaultAuditSuccessText;
            string emailSuccessMessage = Reporting.EmailSuccessTemplate ?? $"<html><head/><body><pre>{LogMessages.DefaultAuditSuccessText}</pre></body></html>";

            LogEventInfo logEvent = new LogEventInfo(LogLevel.Info, Reporting.Logger.Name, ReplaceTokens(tokens, logSuccessMessage, false));
            logEvent.Properties.Add("EventID", EventIDs.PasswordAccessed);
            Reporting.Logger.Log(logEvent);

            try
            {
                var recipients = Reporting.BuildRecipientList(target, authorizationResponse, true, user);

                if (recipients.Count > 0)
                {
                    string subject = ReplaceTokens(tokens, LogMessages.AuditEmailSubjectSuccess, false);
                    Reporting.SendEmail(recipients, subject, ReplaceTokens(tokens, emailSuccessMessage, true));
                }
            }
            catch (Exception iex)
            {
                Reporting.LogErrorEvent(EventIDs.AuditErrorCannotSendSuccessEmail, "An error occurred sending the success audit email", iex);
            }
        }

        public static void PerformAuditFailureActions(LapRequestModel model, string userMessage, int eventID, string logMessage, Exception ex, TargetElement target, AuthorizationResponse authorizationResponse, UserPrincipal user, ComputerPrincipal computer)
        {
            Dictionary<string, string> tokens = BuildTokenDictionary(target, authorizationResponse, user, computer, null, model.ComputerName, logMessage ?? userMessage);
            string logFailureMessage = Reporting.LogFailureTemplate ?? LogMessages.DefaultAuditFailureText;
            string emailFailureMessage = Reporting.EmailFailureTemplate ?? $"<html><head/><body><pre>{LogMessages.DefaultAuditFailureText}</pre></body></html>";

            Reporting.LogErrorEvent(eventID, ReplaceTokens(tokens, logFailureMessage, false), ex);

            try
            {
                var recipients = Reporting.BuildRecipientList(target, authorizationResponse, false);

                if (recipients.Count > 0)
                {
                    string subject = ReplaceTokens(tokens, LogMessages.AuditEmailSubjectFailure, false);
                    Reporting.SendEmail(recipients, subject, ReplaceTokens(tokens, emailFailureMessage, true));
                }
            }
            catch (Exception iex)
            {
                Reporting.LogErrorEvent(EventIDs.AuditErrorCannotSendFailureEmail, "An error occurred sending the failure audit email", iex);
            }
        }

        private static Dictionary<string, string> BuildTokenDictionary(TargetElement target = null, AuthorizationResponse authorizationResponse = null, UserPrincipal user = null, ComputerPrincipal computer = null, SearchResult directoryEntry = null, string requestedComputerName = null, string detailMessage = null)
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
                { "{reader.Principal}", authorizationResponse?.UserDetails},
                { "{reader.Notify}", string.Join(",", authorizationResponse?.UsersToNotify?.All ?? ImmutableHashSet<string>.Empty)},
                { "{target.Notify}", target?.Audit?.EmailAddresses},
                { "{target.ID}", target?.Name},
                { "{target.IDType}", target?.Type.ToString()},
                { "{message}", detailMessage},
                { "{request.IPAddress}", HttpContext.Current?.Request?.UserHostAddress},
                { "{request.HostName}", HttpContext.Current?.Request?.UserHostName},
                { "{request.Xff}", HttpContext.Current?.Request?.GetXffIP()},
                { "{request.XffAll}", HttpContext.Current?.Request?.GetXffList()},
                { "{request.UnmaskedIPAddress}", HttpContext.Current?.Request?.GetUnmaskedIP()},
                { "{datetime}", DateTime.Now.ToString(CultureInfo.CurrentCulture)},
                { "{datetimeutc}", DateTime.UtcNow.ToString(CultureInfo.CurrentCulture)},
                { "{computer.LapsExpiryDate}", directoryEntry?.GetPropertyDateTimeFromLong(Directory.AttrMsMcsAdmPwdExpirationTime)?.ToString(CultureInfo.CurrentCulture)},
            };

            return pairs;
        }

        private static string ReplaceTokens(Dictionary<string, string> tokens, string text, bool isHtml)
        {
            foreach (KeyValuePair<string, string> token in tokens)
            {
                text = text.Replace(token.Key, isHtml ? HttpUtility.HtmlEncode(token.Value) : token.Value);
            }

            return text;
        }

        private static void SendEmail(IEnumerable<string> recipients, string subject, string body)
        {
            if (!Reporting.IsSmtpConfigured())
            {
                Logger.Trace("SMTP is not configured, discarding mail message");
                return;
            }

            using (SmtpClient client = new SmtpClient())
            {
                using (MailMessage message = new MailMessage())
                {
                    foreach (string recipient in recipients)
                    {
                        message.To.Add(recipient);
                    }

                    if (message.To.Count == 0)
                    {
                        Reporting.Logger.Trace($"Not sending notification email because there are no recipients");
                        return;
                    }

                    message.IsBodyHtml = true;
                    message.Subject = subject;
                    message.Body = body;

                    client.Send(message);
                }
            }
        }

        private static IImmutableSet<string> BuildRecipientList(TargetElement target, AuthorizationResponse authorizationResponse, bool success, UserPrincipal user = null)
        {
            // TODO: Avoid having to pass a TargetElement to this function.
            // TODO: Make this testable.
            var usersToNotify = target?.Audit?.UsersToNotify ?? new UsersToNotify();

            if (authorizationResponse != null)
            {
                usersToNotify = usersToNotify.Union(authorizationResponse.UsersToNotify);
            }

            if (LapsConfigSection.Configuration?.Audit?.UsersToNotify != null)
            {
                usersToNotify = usersToNotify.Union(LapsConfigSection.Configuration.Audit.UsersToNotify);
            }

            if (!string.IsNullOrWhiteSpace(user?.EmailAddress))
            {
                // FIXME: This seems to be an undocumented feature?
                usersToNotify = usersToNotify.WithUserReplaced("{user.EmailAddress}", user?.EmailAddress);
            }

            return success ? usersToNotify.OnSuccess : usersToNotify.OnFailure;
        }

        private static string LoadTemplate(string templateName)
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

        private static bool IsSmtpConfigured()
        {
            return !string.IsNullOrWhiteSpace(new SmtpClient().Host);
        }
    }
}