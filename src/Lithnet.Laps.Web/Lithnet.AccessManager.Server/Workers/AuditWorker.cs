using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Lithnet.AccessManager.Server.Auditing;
using Lithnet.AccessManager.Server.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lithnet.AccessManager.Server.Workers
{
    public class AuditWorker : BackgroundService
    {
        private readonly ILogger logger;

        private readonly ChannelReader<Action> channel;

        public AuditWorker(ILogger<AuditWorker> logger, ChannelReader<Action> channel)
        {
            this.logger = logger;
            this.channel = channel;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            this.logger.LogTrace("Starting audit worker background processing thread");
            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            this.logger.LogTrace("Stopping audit worker background processing thread");
            return base.StopAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            await foreach (var item in this.channel.ReadAllAsync(cancellationToken))
            {
                try
                {
                    this.logger.LogTrace("Processing action from queue");
                    item.Invoke();
                }
                catch(OperationCanceledException)
                {
                }
                catch (Exception e)
                {
                    logger.LogEventError(EventIDs.BackgroundTaskUnhandledError, "An unhandled exception occurred in an audit worker background task", e);
                }
            }
        }
    }
}
