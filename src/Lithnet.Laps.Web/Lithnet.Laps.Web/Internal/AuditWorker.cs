using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using NLog;

namespace Lithnet.Laps.Web.Internal
{
    public class AuditWorker : BackgroundService
    {
        private readonly ILogger logger;

        private readonly ChannelReader<Action> channel;

        public AuditWorker(ILogger logger, ChannelReader<Action> channel)
        {
            this.logger = logger;
            this.channel = channel;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            this.logger.Trace("Starting background processing thread");
            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            this.logger.Trace("Stopping background processing thread");
            return base.StopAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            await foreach (var item in this.channel.ReadAllAsync(cancellationToken))
            {
                try
                {
                    this.logger.Trace("Processing action from background queue");
                    item.Invoke();
                }
                catch (Exception e)
                {
                    logger.Error(e, "An unhandled exception occured");
                }
            }
        }
    }
}
