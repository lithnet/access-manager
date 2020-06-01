using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using Lithnet.Laps.Web.App_LocalResources;
using Lithnet.Laps.Web.AppSettings;
using Lithnet.Laps.Web.ActiveDirectory;
using Lithnet.Laps.Web.Authorization;
using Lithnet.Laps.Web.Models;
using NLog;
using Microsoft.AspNetCore.Http;
using System.Net;

namespace Lithnet.Laps.Web.Internal
{
    public sealed class Reporting : IReporting
    {
        private readonly ILogger logger;
        private readonly IMailer mailer;
        private readonly ITemplates templates;
        private readonly IIpAddressResolver ipResolver;
        private readonly GlobalAuditSettings globalAuditSettings;

        public Reporting(ILogger logger, IMailer mailer, ITemplates templates, GlobalAuditSettings globalAuditSettings, IIpAddressResolver ipResolver)
        {
            this.logger = logger;
            this.mailer = mailer;
            this.templates = templates;
            this.ipResolver = ipResolver;
            this.globalAuditSettings = globalAuditSettings;
        }

        public void LogErrorEvent(int eventID, string logMessage, Exception ex)
        {
            LogEventInfo logEvent = new LogEventInfo(LogLevel.Error, this.logger.Name, logMessage);
            logEvent.Properties.Add("EventID", eventID);
            logEvent.Exception = ex;

            this.logger.Log(logEvent);
        }

        public void LogSuccessEvent(int eventID, string logMessage)
        {
            LogEventInfo logEvent = new LogEventInfo(LogLevel.Error, this.logger.Name, logMessage);
            logEvent.Properties.Add("EventID", eventID);

            this.logger.Info(logEvent);
        }

        public void PerformAuditSuccessActions(LapRequestModel model, AuthorizationResponse authorizationResponse, IUser user, IComputer computer, PasswordData passwordData)
        {
            Dictionary<string, string> tokens = this.BuildTokenDictionary(authorizationResponse, user, computer, passwordData, model.ComputerName, null, model.UserRequestReason);
            string logSuccessMessage = this.templates.LogSuccessTemplate ?? LogMessages.DefaultAuditSuccessText;
            string emailSuccessMessage = this.templates.EmailSuccessTemplate ?? $"<html><head/><body><pre>{LogMessages.DefaultAuditSuccessText}</pre></body></html>";

            LogEventInfo logEvent = new LogEventInfo(LogLevel.Info, this.logger.Name, this.ReplaceTokens(tokens, logSuccessMessage, false));
            logEvent.Properties.Add("EventID", EventIDs.PasswordAccessed);
            this.logger.Log(logEvent);

            try
            {
                IImmutableSet<string> recipients = this.BuildRecipientList(authorizationResponse, true, user);

                if (recipients.Count > 0)
                {
                    string subject = this.ReplaceTokens(tokens, LogMessages.AuditEmailSubjectSuccess, false);
                    this.mailer.SendEmail(recipients, subject, this.ReplaceTokens(tokens, emailSuccessMessage, true));
                }
            }
            catch (Exception iex)
            {
                this.LogErrorEvent(EventIDs.AuditErrorCannotSendSuccessEmail, "An error occurred sending the success audit email", iex);
            }
        }

        public void PerformAuditFailureActions(LapRequestModel model, string userMessage, int eventID, string logMessage, Exception ex, AuthorizationResponse authorizationResponse, IUser user, IComputer computer)
        {
            Dictionary<string, string> tokens = this.BuildTokenDictionary(authorizationResponse, user, computer, null, model.ComputerName, logMessage ?? userMessage, model.UserRequestReason);
            string logFailureMessage = this.templates.LogFailureTemplate ?? LogMessages.DefaultAuditFailureText;
            string emailFailureMessage = this.templates.EmailFailureTemplate ?? $"<html><head/><body><pre>{LogMessages.DefaultAuditFailureText}</pre></body></html>";

            this.LogErrorEvent(eventID, this.ReplaceTokens(tokens, logFailureMessage, false), ex);

            try
            {
                IImmutableSet<string> recipients = this.BuildRecipientList(authorizationResponse, false);

                if (recipients.Count > 0)
                {
                    string subject = this.ReplaceTokens(tokens, LogMessages.AuditEmailSubjectFailure, false);
                    this.mailer.SendEmail(recipients, subject, this.ReplaceTokens(tokens, emailFailureMessage, true));
                }
            }
            catch (Exception iex)
            {
                this.LogErrorEvent(EventIDs.AuditErrorCannotSendFailureEmail, "An error occurred sending the failure audit email", iex);
            }
        }

        private Dictionary<string, string> BuildTokenDictionary(AuthorizationResponse authorizationResponse = null, IUser user = null, IComputer computer = null, PasswordData passwordData = null, string requestedComputerName = null, string detailMessage = null, string requestedReason = null, HttpRequest request = null)
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
                { "{requestedReason}", requestedReason},
                { "{authzresult.MatchedAcePrincipal}", authorizationResponse?.MatchedAcePrincipal},
                { "{authzresult.NotificationRecipients}", string.Join(",", authorizationResponse?.NotificationRecipients ?? new List<string>())},
                { "{authzresult.MatchedRuleDescription}", authorizationResponse?.MatchedRuleDescription},
                { "{authzresult.AdditionalInformation}", authorizationResponse?.AdditionalInformation},
                { "{authzresult.ExpireAfter}", authorizationResponse?.ExpireAfter.ToString()},
                { "{authzresult.ResponseCode}", authorizationResponse?.Code.ToString()},
                { "{message}", detailMessage},
                { "{request.IPAddress}", request?.HttpContext?.Connection?.RemoteIpAddress?.ToString()},
                { "{request.ResolvedIPAddress}", request == null ? null : this.ipResolver.GetRequestIP(request)},
                { "{datetime}", DateTime.Now.ToString(CultureInfo.CurrentCulture)},
                { "{datetimeutc}", DateTime.UtcNow.ToString(CultureInfo.CurrentCulture)},
                { "{computer.LapsExpiryDate}", passwordData?.ExpirationTime?.ToString(CultureInfo.CurrentCulture)},
            };

            return pairs;
        }

        private string ReplaceTokens(Dictionary<string, string> tokens, string text, bool isHtml)
        {
            foreach (KeyValuePair<string, string> token in tokens)
            {
                text = text.Replace(token.Key, isHtml ? WebUtility.HtmlEncode(token.Value) : token.Value);
            }

            return text;
        }

        private IImmutableSet<string> BuildRecipientList(AuthorizationResponse authorizationResponse, bool success, IUser user = null)
        {
            HashSet<string> usersToNotify = new HashSet<string>();

            if (authorizationResponse != null)
            {
                authorizationResponse.NotificationRecipients?.ForEach(t => usersToNotify.Add(t));
            }
            
            if (success)
            {
                this.globalAuditSettings.SuccessRecipients?.ForEach( t => usersToNotify.Add(t));
            }
            else
            {
                this.globalAuditSettings.FailureRecipients?.ForEach(t => usersToNotify.Add(t));
            }
            
            if (!string.IsNullOrWhiteSpace(user?.EmailAddress))
            {
                if (usersToNotify.Remove("{user.EmailAddress}"))
                {
                    usersToNotify.Add(user.EmailAddress);
                }
            }

            return usersToNotify.ToImmutableHashSet();
        }
    }
}