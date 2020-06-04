using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Net.Http;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using Lithnet.Laps.Web.AppSettings;
using Lithnet.Laps.Web.Exceptions;
using Microsoft.AspNetCore.Hosting;
using NLog;

namespace Lithnet.Laps.Web.Internal
{
    public abstract class NotificationChannel<T> : INotificationChannel where T : IChannelSettings
    {
        private readonly ILogger logger;

        private readonly ChannelWriter<Action> queue;

        public abstract string Name { get; }

        public NotificationChannel(ILogger logger, ChannelWriter<Action> queue)
        {
            this.logger = logger;
            this.queue = queue;
        }

        public abstract void ProcessNotification(AuditableAction action, Dictionary<string, string> tokens, IImmutableSet<string> notificationChannels);

        protected void ProcessNotification(AuditableAction action, Dictionary<string, string> tokens, IImmutableSet<string> notificationChannelIDs, IEnumerable<T> notificationChannelSettings)
        {
            List<Exception> rethrowableExceptions = new List<Exception>();

            foreach (var channel in notificationChannelSettings)
            {
                if (notificationChannelIDs.Any(t => string.Equals(t, channel.ID, StringComparison.OrdinalIgnoreCase)))
                {
                    if (!channel.Enabled)
                    {
                        this.logger.Trace($"Skipping delivery of audit notification to {channel.ID} as it is currently disabled");
                        continue;
                    }

                    try
                    {
                        if (channel.DenyOnAuditError)
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
                this.logger.Trace($"Attempting delivery of audit notification to {channel.ID}");
                this.Send(action, tokens, channel);
                this.logger.Trace($"Delivery of audit notification to {channel.ID} successful");
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, $"Delivery of audit notification to {channel.ID} failed");

                if (rethrowExceptions)
                {
                    throw;
                }
            }
        }
    }
}