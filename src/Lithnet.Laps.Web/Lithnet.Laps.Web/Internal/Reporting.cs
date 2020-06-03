using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using Lithnet.Laps.Web.App_LocalResources;
using Lithnet.Laps.Web.AppSettings;
using NLog;
using Microsoft.AspNetCore.Http;
using System.Net;
using System.Threading.Tasks.Dataflow;
using System.Diagnostics;

namespace Lithnet.Laps.Web.Internal
{
    public sealed class Reporting : IReporting
    {
        private readonly ILogger logger;
        private readonly ITemplates templates;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IEnumerable<INotificationChannel> notificationChannels;

        public Reporting(ILogger logger, ITemplates templates, IEnumerable<INotificationChannel> notificationChannels, IHttpContextAccessor httpContextAccessor)
        {
            this.logger = logger;
            this.templates = templates;
            this.httpContextAccessor = httpContextAccessor;
            this.notificationChannels = notificationChannels;
        }

        public void LogEventError(int eventId, string logMessage)
        {
            this.LogEventError(eventId, logMessage, null);
        }

        public void LogEventError(int eventId, string message, Exception ex)
        {
            this.LogEvent(eventId, LogLevel.Error, message, ex);
        }

        public void LogEventSuccess(int eventId, string message)
        {
            this.LogEvent(eventId, LogLevel.Info, message, null);
        }

        public void LogEvent(int eventId, LogLevel logLevel, string message, Exception ex)
        {
            LogEventInfo logEvent = new LogEventInfo(logLevel, this.logger.Name, message);
            logEvent.Properties.Add("EventID", eventId);
            logEvent.Exception = ex;

            this.logger.Info(logEvent);
        }

        public void GenerateAuditEvent(AuditableAction action)
        {
            Dictionary<string, string> tokens = this.BuildTokenDictionary(action);

            this.GenerateAuditEventLog(action, tokens);

            foreach (INotificationChannel channel in this.notificationChannels)
            {
                try
                {
                    channel.ProcessNotification(action, tokens);
                }
                catch (Exception ex)
                {
                    this.LogEventError(EventIDs.NotificationChannelError, string.Format(LogMessages.NotificationChannelError, channel.Name), ex);
                }
            }
        }

        private void GenerateAuditEventLog(AuditableAction action, Dictionary<string, string> tokens)
        {
            string message;

            if (action.IsSuccess)
            {
                message = this.templates.LogSuccessTemplate ?? LogMessages.DefaultAuditSuccessText;
            }
            else
            {
                message = this.templates.LogFailureTemplate ?? LogMessages.DefaultAuditFailureText;
            }

            message = this.ReplaceTokens(tokens, message, false);

            this.LogEvent(action.EventID, action.IsSuccess ? LogLevel.Info : LogLevel.Error, message, null);
        }

        private Dictionary<string, string> BuildTokenDictionary(AuditableAction action)
        {
            Dictionary<string, string> pairs = new Dictionary<string, string> {
                { "{user.SamAccountName}", action.User?.SamAccountName},
                { "{user.DisplayName}", action.User?.DisplayName},
                { "{user.UserPrincipalName}", action.User?.UserPrincipalName},
                { "{user.Sid}", action.User?.Sid?.ToString()},
                { "{user.DistinguishedName}", action.User?.DistinguishedName},
                { "{user.Description}", action.User?.Description},
                { "{user.EmailAddress}", action.User?.EmailAddress},
                { "{user.Guid}", action.User?.Guid?.ToString()},
                { "{user.GivenName}", action.User?.GivenName},
                { "{user.Surname}", action.User?.Surname},
                { "{computer.SamAccountName}", action.Computer?.SamAccountName},
                { "{computer.DistinguishedName}", action.Computer?.DistinguishedName},
                { "{computer.Description}", action.Computer?.Description},
                { "{computer.DisplayName}", action.Computer?.DisplayName},
                { "{computer.Guid}", action.Computer?.Guid?.ToString()},
                { "{computer.Sid}", action.Computer?.Sid?.ToString()},
                { "{requestedComputerName}", action.RequestModel?.ComputerName},
                { "{requestedReason}", action.RequestModel?.UserRequestReason},
                { "{authzresult.MatchedAcePrincipal}", action.AuthzResponse?.MatchedAcePrincipal},
                { "{authzresult.NotificationRecipients}", string.Join(",", action.AuthzResponse?.NotificationRecipients ?? new List<string>())},
                { "{authzresult.MatchedRuleDescription}", action.AuthzResponse?.MatchedRuleDescription},
                { "{authzresult.AdditionalInformation}", action.AuthzResponse?.AdditionalInformation},
                { "{authzresult.ExpireAfter}", action.AuthzResponse?.ExpireAfter.ToString()},
                { "{authzresult.ResponseCode}", action.AuthzResponse?.Code.ToString()},
                { "{message}", action.Message},
                { "{request.IPAddress}", httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString()},
                { "{request.Hostname}", this.TryResolveHostName(httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress)},
                { "{datetime}", DateTime.Now.ToString(CultureInfo.CurrentCulture)},
                { "{datetimeutc}", DateTime.UtcNow.ToString(CultureInfo.CurrentCulture)},
                { "{computer.LapsExpiryDate}", action.ComputerExpiryDate},
            };

            return pairs;
        }

        private string ReplaceTokens(Dictionary<string, string> tokens, string text, bool isHtml)
        {
            foreach (KeyValuePair<string, string> token in tokens)
            {
                text = text.Replace(token.Key, isHtml ? WebUtility.HtmlEncode(token.Value) : token.Value, StringComparison.OrdinalIgnoreCase);
            }

            return text;
        }

        private string TryResolveHostName(IPAddress address)
        {
            if (address == null)
            {
                return null;
            }

            try
            {
                return Dns.GetHostEntry(address)?.HostName;
            }
            catch
            {
                return null;
            }
        }
    }
}