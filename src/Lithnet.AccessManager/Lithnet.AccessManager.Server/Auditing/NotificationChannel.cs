using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Channels;
using Lithnet.AccessManager.Server.Configuration;
using Lithnet.AccessManager.Server.Exceptions;
using Lithnet.AccessManager.Server.Extensions;
using Microsoft.Extensions.Logging;

namespace Lithnet.AccessManager.Server.Auditing
{
    public abstract class NotificationChannel<T> : INotificationChannel where T : NotificationChannelDefinition
    {
        private readonly ILogger logger;

        private readonly ChannelWriter<Action> queue;

        public abstract string Name { get; }

        protected abstract IList<T> NotificationChannelDefinitions { get; }

        protected NotificationChannel(ILogger logger, ChannelWriter<Action> queue)
        {
            this.logger = logger;
            this.queue = queue;
        }

        public void ProcessNotification(AuditableAction action, Dictionary<string, string> tokens, IImmutableSet<string> notificationChannelIDs)
        {
            List<Exception> rethrowableExceptions = new List<Exception>();

            foreach (var channel in this.NotificationChannelDefinitions)
            {
                if (notificationChannelIDs.Any(t => string.Equals(t, channel.Id, StringComparison.OrdinalIgnoreCase)))
                {
                    if (!channel.Enabled)
                    {
                        this.logger.LogTrace($"Skipping delivery of audit notification to {channel.Id} as it is currently disabled");
                        continue;
                    }

                    try
                    {
                        if (channel.Mandatory && action.IsSuccess)
                        {
                            Send(action, tokens, channel, true);
                        }
                        else
                        {
                            this.queue.TryWrite(() => Send(action, tokens, channel, false));
                        }
                    }
                    catch (Exception ex)
                    {
                        rethrowableExceptions.Add(ex);
                    }
                }
            }

            if (action.IsSuccess && rethrowableExceptions.Count > 0)
            {
                throw new AuditLogFailureException(rethrowableExceptions);
            }
        }

        protected abstract void Send(AuditableAction action, Dictionary<string, string> tokens, T settings);

        private void Send(AuditableAction action, Dictionary<string, string> tokens, T channel, bool rethrowExceptions)
        {
            try
            {
                this.logger.LogTrace($"Attempting delivery of audit notification to {channel.Id}");
                this.Send(action, tokens, channel);
                this.logger.LogTrace($"Delivery of audit notification to {channel.Id} successful");
            }
            catch (Exception ex)
            {
                this.logger.LogEventError(EventIDs.NotificationChannelError, $"Delivery of audit notification to {channel.Id} failed", ex);

                if (rethrowExceptions)
                {
                    throw;
                }
            }
        }
    }
}