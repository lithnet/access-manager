using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using Quartz;
using Quartz.Impl;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Server.Test
{
    public class SchedulerTests
    {
        IScheduler scheduler;

        [SetUp()]
        public async Task TestInitialize()
        {
            StdSchedulerFactory factory = new StdSchedulerFactory();
            factory.Initialize(new System.Collections.Specialized.NameValueCollection()
            {
                { "quartz.jobStore.type", "Quartz.Impl.AdoJobStore.JobStoreTX, Quartz" } ,
                { "quartz.jobStore.driverDelegateType", "Quartz.Impl.AdoJobStore.SqlServerDelegate, Quartz" } ,
                { "quartz.dataSource.mydb.provider", "SqlServer" } ,
                { "quartz.dataSource.mydb.connectionString", @"Data Source=CARBON\SQLEXPRESS;Initial Catalog=AccessManager;Integrated Security=True" } ,
                { "quartz.jobStore.dataSource", "mydb" } ,
                { "quartz.serializer.type", "json" } ,
            });

            scheduler = await factory.GetScheduler();

            await scheduler.Start();
        }

        [Test]
        public async Task TestScheduler()
        {
            if (!await scheduler.CheckExists(new JobKey("job1", "group1")))
            {

                IJobDetail job = JobBuilder.Create<HelloJob>()
                         .WithIdentity("job1", "group1")
                         .UsingJobData("Test", "mydata")
                         .Build();

                // Trigger the job to run now, and then repeat every 10 seconds
                ITrigger trigger = TriggerBuilder.Create()
                    .WithIdentity("trigger1", "group1")
                    .StartNow()
                    .WithSimpleSchedule(x => x
                        .WithIntervalInSeconds(10)
                        .RepeatForever())
                    .Build();

                // Tell quartz to schedule the job using our trigger
                await scheduler.ScheduleJob(job, trigger);
            }
            await Task.Delay(TimeSpan.FromSeconds(60));

        }
    }

    public class HelloJob : IJob
    {
        public string Test { get; set; }

        public async Task Execute(IJobExecutionContext context)
        {
            await Console.Out.WriteLineAsync("Greetings from HelloJob!");
        }
    }
}
