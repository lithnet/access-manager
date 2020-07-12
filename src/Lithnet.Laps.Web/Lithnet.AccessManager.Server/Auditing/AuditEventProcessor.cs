using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Net;
using Lithnet.AccessManager.Server.App_LocalResources;
using Lithnet.AccessManager.Server.Configuration;
using Lithnet.AccessManager.Server.Auditing;
using Lithnet.AccessManager.Server.Exceptions;
using Lithnet.AccessManager.Server.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using NLog;
using Lithnet.AccessManager.Server.Authorization;

namespace Lithnet.AccessManager.Web.Internal
{
    public sealed class AuditEventProcessor : IAuditEventProcessor
    {
        private readonly ILogger logger;

        private readonly ITemplateProvider templates;

        private readonly IHttpContextAccessor httpContextAccessor;

        private readonly IEnumerable<INotificationChannel> notificationChannels;

        private readonly AuditOptions auditSettings;

        public AuditEventProcessor(ILogger logger, ITemplateProvider templates, IEnumerable<INotificationChannel> notificationChannels, IHttpContextAccessor httpContextAccessor, IOptions<AuditOptions> auditSettings)
        {
            this.logger = logger;
            this.templates = templates;
            this.httpContextAccessor = httpContextAccessor;
            this.notificationChannels = notificationChannels;
            this.auditSettings = auditSettings.Value;
        }

        public void GenerateAuditEvent(AuditableAction action)
        {
            Dictionary<string, string> tokens = this.BuildTokenDictionary(action);

            List<Exception> exceptions = new List<Exception>();

            foreach (INotificationChannel channel in this.notificationChannels)
            {
                try
                {
                    channel.ProcessNotification(action, tokens, this.BuildChannelList(action));
                }
                catch (Exception ex)
                {
                    this.logger.LogEventError(EventIDs.NotificationChannelError, string.Format(LogMessages.NotificationChannelError, channel.Name), ex);
                    exceptions.Add(ex);
                }
            }

            if (exceptions.Count > 0)
            {
                AuditLogFailureException ex = new AuditLogFailureException("The audit message could not be delivered to all notification channels", exceptions);

                if (action.IsSuccess)
                {
                    throw ex;
                }
                else
                {
                    this.logger.LogEventError(EventIDs.NotificationChannelError, ex.Message, ex);
                }
            }

            this.GenerateAuditEventLog(action, tokens);
        }

        private void GenerateAuditEventLog(AuditableAction action, Dictionary<string, string> tokens)
        {
            string message;

            if (action.IsSuccess)
            {
                message = this.templates.GetTemplate("LogAuditSuccess.txt") ?? LogMessages.DefaultAuditSuccessText;
            }
            else
            {
                message = this.templates.GetTemplate("LogAuditFailure.txt") ?? LogMessages.DefaultAuditFailureText;
            }

            message = this.ReplaceTokens(tokens, message, false);

            this.logger.LogEvent(action.EventID, action.IsSuccess ? LogLevel.Info : LogLevel.Error, message, null);
        }

        private Dictionary<string, string> BuildTokenDictionary(AuditableAction action)
        {
            LapsAuthorizationResponse lapsAuthZResponse = action.AuthzResponse as LapsAuthorizationResponse;
            JitAuthorizationResponse jitAuthZResponse = action.AuthzResponse as JitAuthorizationResponse;
            
            Dictionary<string, string> pairs = new Dictionary<string, string> {
                { "{user.SamAccountName}", action.User?.SamAccountName},
                { "{user.MsDsPrincipalName}", action.User?.MsDsPrincipalName},
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
                { "{computer.MsDsPrincipalName}", action.Computer?.MsDsPrincipalName},
                { "{computer.DistinguishedName}", action.Computer?.DistinguishedName},
                { "{computer.Description}", action.Computer?.Description},
                { "{computer.DisplayName}", action.Computer?.DisplayName},
                { "{computer.Guid}", action.Computer?.Guid?.ToString()},
                { "{computer.Sid}", action.Computer?.Sid?.ToString()},
                { "{request.ComputerName}", action.RequestedComputerName},
                { "{request.Reason}", action.RequestReason ?? "(not provided)"},
                { "{AuthzResult.NotificationChannels}", string.Join(",", action.AuthzResponse?.NotificationChannels ?? new List<string>())},
                { "{AuthzResult.MatchedRuleDescription}", action.AuthzResponse?.MatchedRuleDescription},
                { "{AuthzResult.MatchedRule}", action.AuthzResponse?.MatchedRule},
                { "{AuthzResult.ExpireAfter}", (lapsAuthZResponse?.ExpireAfter ?? jitAuthZResponse?.ExpireAfter)?.ToString()},
                { "{AuthzResult.ResponseCode}", action.AuthzResponse?.Code.ToString()},
                { "{AuthzResult.AccessType}", action.AuthzResponse?.EvaluatedAccess.ToDescription()},
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

        private IImmutableSet<string> BuildChannelList(AuditableAction action)
        {
            HashSet<string> channelsToNotify = new HashSet<string>();

            if (action.AuthzResponse != null)
            {
                action.AuthzResponse.NotificationChannels?.ForEach(t => channelsToNotify.Add(t));
            }

            if (action.IsSuccess)
            {
                this.auditSettings.GlobalNotifications?.OnSuccess?.ForEach(t => channelsToNotify.Add(t));
            }
            else
            {
                this.auditSettings.GlobalNotifications?.OnFailure?.ForEach(t => channelsToNotify.Add(t));
            }

            return channelsToNotify.ToImmutableHashSet();
        }
    }
}