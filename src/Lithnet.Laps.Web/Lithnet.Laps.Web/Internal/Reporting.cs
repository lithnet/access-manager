using System;
using System.Collections.Generic;
using System.Configuration;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Globalization;
using System.Linq;
using System.Net.Configuration;
using System.Net.Mail;
using System.Web;
using System.Web.Configuration;
using Lithnet.Laps.Web.App_LocalResources;
using Lithnet.Laps.Web.Models;
using NLog;

namespace Lithnet.Laps.Web
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

        public static void PerformAuditSuccessActions(LapRequestModel model, TargetElement target, ReaderElement reader, UserPrincipal user, ComputerPrincipal computer, SearchResult searchResult)
        {
            Dictionary<string, string> tokens = BuildTokenDictionary(target, reader, user, computer, searchResult, model.ComputerName);
            string logSuccessMessage = Reporting.LogSuccessTemplate ?? LogMessages.DefaultAuditSuccessText;
            string emailSuccessMessage = Reporting.EmailSuccessTemplate ?? $"<html><head/><body><pre>{LogMessages.DefaultAuditSuccessText}</pre></body></html>";

            LogEventInfo logEvent = new LogEventInfo(LogLevel.Info, Reporting.Logger.Name, ReplaceTokens(tokens, logSuccessMessage, false));
            logEvent.Properties.Add("EventID", EventIDs.PasswordAccessed);
            Reporting.Logger.Log(logEvent);

            try
            {
                ICollection<string> recipients = Reporting.BuildRecipientList(target, reader, true, user);

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

        public static void PerformAuditFailureActions(LapRequestModel model, string userMessage, int eventID, string logMessage, Exception ex, TargetElement target, ReaderElement reader, UserPrincipal user, ComputerPrincipal computer)
        {
            Dictionary<string, string> tokens = BuildTokenDictionary(target, reader, user, computer, null, model.ComputerName, logMessage ?? userMessage);
            string logFailureMessage = Reporting.LogFailureTemplate ?? LogMessages.DefaultAuditFailureText;
            string emailFailureMessage = Reporting.EmailFailureTemplate ?? $"<html><head/><body><pre>{LogMessages.DefaultAuditFailureText}</pre></body></html>";

            Reporting.LogErrorEvent(eventID, ReplaceTokens(tokens, logFailureMessage, false), ex);

            try
            {
                ICollection<string> recipients = Reporting.BuildRecipientList(target, reader, false);

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

        private static Dictionary<string, string> BuildTokenDictionary(TargetElement target = null, ReaderElement reader = null, UserPrincipal user = null, ComputerPrincipal computer = null, SearchResult directoryEntry = null, string requestedComputerName = null, string detailMessage = null)
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
                { "{reader.Principal}", reader?.Principal},
                { "{reader.Notify}", reader?.Audit?.EmailAddresses},
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

        private static ICollection<string> BuildRecipientList(TargetElement target, ReaderElement reader, bool success, UserPrincipal user = null)
        {
            HashSet<string> list = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);
            if ((success && (target?.Audit?.NotifySuccess ?? false))
                || (!success && (target?.Audit?.NotifyFailure ?? false)))
            {
                Reporting.SplitRecipientsAndAddtoList(target?.Audit?.EmailAddresses, list);
            }

            if ((success && (reader?.Audit?.NotifySuccess ?? false))
                || (!success && (reader?.Audit?.NotifyFailure ?? false)))
            {
                Reporting.SplitRecipientsAndAddtoList(reader?.Audit?.EmailAddresses, list);
            }

            if ((success && (LapsConfigSection.Configuration?.Audit?.NotifySuccess ?? false))
                || (!success && (LapsConfigSection.Configuration?.Audit?.NotifyFailure ?? false)))
            {
                Reporting.SplitRecipientsAndAddtoList(LapsConfigSection.Configuration?.Audit?.EmailAddresses, list);
            }

            if (list.Remove("{user.EmailAddress}"))
            {
                if (!string.IsNullOrWhiteSpace(user?.EmailAddress))
                {
                    list.Add(user.EmailAddress);
                }
            }

            return list;
        }

        private static void SplitRecipientsAndAddtoList(string recepientList, HashSet<string> list)
        {
            if (string.IsNullOrWhiteSpace(recepientList))
            {
                return;
            }

            foreach (string item in recepientList.Split(new char[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                list.Add(item);
            }
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