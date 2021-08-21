using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Impl.Matchers;

namespace Lithnet.AccessManager.Server
{
    public class SchedulerService : IHostedService
    {
        private readonly ILogger logger;
        private readonly ISchedulerFactory schedulerFactory;
        private readonly IRegistryProvider registryProvider;
        private IScheduler scheduler;
        public const string MaintenanceGroupName = "Maintenance";

        public SchedulerService(ILogger<SchedulerService> logger, ISchedulerFactory schedulerFactory, IRegistryProvider registryProvider)
        {
            this.logger = logger;
            this.schedulerFactory = schedulerFactory;
            this.registryProvider = registryProvider;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            this.logger.LogTrace("Starting scheduler background processing thread");
            scheduler = await schedulerFactory.GetScheduler(cancellationToken);
            await this.SetupJobsAsync();

            await scheduler.Start(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            this.logger.LogTrace("Stopping scheduler background processing thread");
            return scheduler.Shutdown(false, cancellationToken);
        }

        private async Task SetupJobsAsync()
        {
            if (this.registryProvider.ResetScheduler)
            {
                await this.scheduler.Clear();
                this.registryProvider.ResetScheduler = false;
                this.registryProvider.ResetMaintenanceTaskSchedules = false;
            }
            else if (this.registryProvider.ResetMaintenanceTaskSchedules)
            {
                foreach (var job in await this.scheduler.GetJobKeys(GroupMatcher<JobKey>.GroupEquals(MaintenanceGroupName)))
                {
                    await this.scheduler.DeleteJob(job);
                }

                this.registryProvider.ResetMaintenanceTaskSchedules = false;
            }

            await CertificateExpiryCheckJob.EnsureCreated(this.scheduler);
            await NewVersionCheckJob.EnsureCreated(this.scheduler);
            await DbMaintenanceJob.EnsureCreated(this.scheduler);
            await DbBackupJob.EnsureCreated(this.scheduler);
        }
    }
}
