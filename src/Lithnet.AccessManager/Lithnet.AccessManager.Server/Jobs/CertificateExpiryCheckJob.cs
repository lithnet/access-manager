using Quartz;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Server
{
    public class CertificateExpiryCheckJob : IJob
    {
        public static readonly JobKey JobKey = new JobKey("CertificateExpiryCheckJob", "RegularMaintenaceTasks");

        public static readonly TriggerKey TriggerKey = new TriggerKey("Daily4AM", "CertificateExpiryCheckJobTriggers");

        public Task Execute(IJobExecutionContext context)
        {
            return Task.CompletedTask;
        }

        public static async Task EnsureCreated(IScheduler scheduler)
        {
            if (await scheduler.CheckExists(JobKey))
            {
                return;
            }

            IJobDetail job = JobBuilder.Create<CertificateExpiryCheckJob>()
                     .WithIdentity(JobKey)
                     .Build();

            ITrigger trigger;

            if (!await scheduler.CheckExists(TriggerKey))
            {
                trigger = TriggerBuilder.Create()
                    .WithIdentity(TriggerKey)
                    .StartNow()
                    .WithDailyTimeIntervalSchedule(x => x
                        .StartingDailyAt(TimeOfDay.HourAndMinuteOfDay(4, 0))
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
