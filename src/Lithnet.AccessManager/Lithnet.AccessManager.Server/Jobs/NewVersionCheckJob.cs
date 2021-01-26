using HtmlAgilityPack;
using Lithnet.AccessManager.Server.Auditing;
using Lithnet.AccessManager.Server.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Server
{
    [DisallowConcurrentExecution]
    public class NewVersionCheckJob : IJob
    {
        private const string JobName = "NewVersionCheck";
        public static readonly JobKey JobKey = new JobKey($"{JobName}Job", SchedulerService.MaintenanceGroupName);
        public static readonly TriggerKey TriggerKey = new TriggerKey($"{JobName}Trigger", SchedulerService.MaintenanceGroupName);

        private readonly IApplicationUpgradeProvider appUpgradeProvider;
        private readonly ILogger<NewVersionCheckJob> logger;
        private readonly IRegistryProvider registryProvider;
        private readonly ISmtpProvider smtpProvider;
        private readonly IOptionsMonitor<AdminNotificationOptions> adminNotificationOptions;

        public NewVersionCheckJob(IApplicationUpgradeProvider appUpgradeProvider, ILogger<NewVersionCheckJob> logger, ISmtpProvider smtpProvider, IOptionsMonitor<AdminNotificationOptions> adminNotificationOptions, IRegistryProvider registryProvider)
        {
            this.appUpgradeProvider = appUpgradeProvider;
            this.logger = logger;
            this.registryProvider = registryProvider;
            this.smtpProvider = smtpProvider;
            this.adminNotificationOptions = adminNotificationOptions;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                AdminNotificationOptions emailOptions = this.adminNotificationOptions.CurrentValue;

                if (!emailOptions.EnableNewVersionAlerts || string.IsNullOrWhiteSpace(emailOptions.AdminAlertRecipients))
                {
                    return;
                }

                if (!this.smtpProvider.IsConfigured)
                {
                    return;
                }

                var versionInfo = await this.appUpgradeProvider.GetVersionInfo();

                if (versionInfo.Status != VersionInfoStatus.UpdateAvailable)
                {
                    return;
                }

                if (this.registryProvider.LastNotifiedVersion == versionInfo.AvailableVersion.ToString())
                {
                    return;
                }

                this.registryProvider.LastNotifiedVersion = versionInfo.AvailableVersion.ToString();

                this.Send(versionInfo, emailOptions.AdminAlertRecipients);
            }
            catch (Exception ex)
            {
                logger.LogWarning(EventIDs.NewVersionCheckJobFailed, ex, "Could not get version update");
            }
        }

        private void Send(AppVersionInfo versionInfo, string recipients)
        {
            string body = EmbeddedResourceProvider.GetResourceString("NewVersionAvailableEmail.html", "EmbeddedResources.Templates");
            string subject = GetSubjectLine(body) ?? "New version available";

            Dictionary<string, string> tokens = new Dictionary<string, string>()
            {
                { "{url}", versionInfo.UpdateUrl},
                { "{version}" , versionInfo.AvailableVersion.ToString()},
                { "{releaseNotes}" , versionInfo.ReleaseNotes},
            };

            body = TokenReplacer.ReplaceAsHtml(tokens, body);
            subject = TokenReplacer.ReplaceAsPlainText(tokens, subject);

            this.smtpProvider.SendEmail(recipients, subject, body);
        }

        private string GetSubjectLine(string content)
        {
            HtmlDocument d = new HtmlDocument();
            d.LoadHtml(content);

            var titleNode = d.DocumentNode.SelectSingleNode("html/head/title");

            return string.IsNullOrWhiteSpace(titleNode?.InnerText) ? null : titleNode.InnerText;
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
                         .StartingDailyAt(TimeOfDay.HourAndMinuteOfDay(9, 0))
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
