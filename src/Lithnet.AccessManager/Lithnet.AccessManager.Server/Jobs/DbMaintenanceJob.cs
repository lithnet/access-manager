using Microsoft.Extensions.Logging;
using Quartz;
using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Lithnet.AccessManager.Server.Configuration;
using Microsoft.Extensions.Options;

namespace Lithnet.AccessManager.Server
{
    [DisallowConcurrentExecution]
    public class DbMaintenanceJob : IJob
    {
        private const string JobName = "DbMainteanceJob";
        public static readonly JobKey JobKey = new JobKey($"{JobName}Job", SchedulerService.MaintenanceGroupName);
        public static readonly TriggerKey TriggerKey = new TriggerKey($"{JobName}Trigger", SchedulerService.MaintenanceGroupName);

        private readonly ILogger<DbMaintenanceJob> logger;
        private readonly IDbProvider dbProvider;
        private readonly IOptions<DatabaseOptions> dbOptions;

        public DbMaintenanceJob(ILogger<DbMaintenanceJob> logger, IDbProvider dbProvider, IOptions<DatabaseOptions> dbOptions)
        {
            this.logger = logger;
            this.dbProvider = dbProvider;
            this.dbOptions = dbOptions;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                if (!this.dbOptions.Value.EnableBuiltInMaintenance)
                {
                    return;
                }

                this.logger.LogInformation("Running database maintenance");

                await using SqlConnection con = this.dbProvider.GetConnection();

                SqlCommand command = new SqlCommand("IndexOptimize", con);
                command.CommandType = System.Data.CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@Databases", "AccessManager");
                command.Parameters.AddWithValue("@UpdateStatistics", "ALL");
                command.Parameters.AddWithValue("@LockTimeout ", "60");

                con.InfoMessage += delegate (object sender, SqlInfoMessageEventArgs e)
                {
                    this.logger.LogTrace(e.Message);
                };

                await command.ExecuteNonQueryAsync();

                this.logger.LogInformation("Database maintenance completed");
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(EventIDs.CertificateExpiryCheckJobFailed, ex, "Database maintenance failed");
            }
        }

        public static async Task EnsureCreated(IScheduler scheduler)
        {
            if (await scheduler.CheckExists(JobKey))
            {
                return;
            }

            IJobDetail job = JobBuilder.Create<DbMaintenanceJob>()
                     .WithIdentity(JobKey)
                     .Build();

            ITrigger trigger;

            if (!await scheduler.CheckExists(TriggerKey))
            {
                trigger = TriggerBuilder.Create()
                    .WithIdentity(TriggerKey)
                    .StartNow()
                    .WithDailyTimeIntervalSchedule(x => x
                        .StartingDailyAt(TimeOfDay.HourAndMinuteOfDay(1, 0))
                        .WithIntervalInHours(24)
                        .WithMisfireHandlingInstructionFireAndProceed())
                    .Build();
            }
            else
            {
                trigger = await scheduler.GetTrigger(TriggerKey);
            }

            await scheduler.ScheduleJob(job, trigger);
        }
    }
}
