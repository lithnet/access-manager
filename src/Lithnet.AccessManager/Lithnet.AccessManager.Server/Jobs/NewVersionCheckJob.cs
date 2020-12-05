using Quartz;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Server
{
    public class NewVersionCheckJob : IJob
    {
        public static readonly JobKey JobKey = new JobKey("NewVersionCheckJob", "RegularMaintenaceTasks");

        public static readonly TriggerKey TriggerKey = new TriggerKey("Daily4AM", "NewVersionCheckTriggerDaily");


        public Task Execute(IJobExecutionContext context)
        {
            throw new NotImplementedException();
        }


        public static async Task EnsureCreated(IScheduler scheduler)
        {
            if (await scheduler.CheckExists(JobKey))
            {
                return;
            }

            IJobDetail job = JobBuilder.Create<NewVersionCheckJob>()
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
