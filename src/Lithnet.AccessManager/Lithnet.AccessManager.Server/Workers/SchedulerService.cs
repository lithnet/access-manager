using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Lithnet.AccessManager.Server
{
    public class SchedulerService : IHostedService
    {
        private readonly ILogger logger;
        private readonly ISchedulerFactory schedulerFactory;
        private IScheduler scheduler;

        public SchedulerService(ILogger<SchedulerService> logger, ISchedulerFactory schedulerFactory)
        {
            this.logger = logger;
            this.schedulerFactory = schedulerFactory;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            this.logger.LogTrace("Starting scheduler background processing thread");
            scheduler = await schedulerFactory.GetScheduler(cancellationToken);
            await scheduler.Start(cancellationToken);
            await this.SetupJobsAsync();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            this.logger.LogTrace("Stopping scheduler background processing thread");
            return scheduler.Shutdown(false, cancellationToken);
        }

        private async Task SetupJobsAsync()
        {
            await CertificateExpiryCheckJob.EnsureCreated(this.scheduler);
            await NewVersionCheckJob.EnsureCreated(this.scheduler);
        }
    }
}
