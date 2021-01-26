using Lithnet.AccessManager.Server.Auditing;
using Lithnet.AccessManager.Server.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Server
{
    [DisallowConcurrentExecution]
    public class CertificateExpiryCheckJob : IJob
    {
        private const string JobName = "CertificateExpiryCheck";
        public static readonly JobKey JobKey = new JobKey($"{JobName}Job", SchedulerService.MaintenanceGroupName);
        public static readonly TriggerKey TriggerKey = new TriggerKey($"{JobName}Trigger", SchedulerService.MaintenanceGroupName);

        private readonly IHttpSysConfigurationProvider httpSysConfigProvider;
        private readonly ILogger<CertificateExpiryCheckJob> logger;
        private readonly IRegistryProvider registryProvider;
        private readonly ISmtpProvider smtpProvider;
        private readonly IOptionsMonitor<AdminNotificationOptions> adminNotificationOptions;

        public CertificateExpiryCheckJob(IHttpSysConfigurationProvider httpSysConfigProvider, ILogger<CertificateExpiryCheckJob> logger, IRegistryProvider registryProvider, ISmtpProvider smtpProvider, IOptionsMonitor<AdminNotificationOptions> emailOptions)
        {
            this.httpSysConfigProvider = httpSysConfigProvider;
            this.logger = logger;
            this.registryProvider = registryProvider;
            this.smtpProvider = smtpProvider;
            this.adminNotificationOptions = emailOptions;
        }

        public Task Execute(IJobExecutionContext context)
        {
            try
            {
                AdminNotificationOptions emailOptions = this.adminNotificationOptions.CurrentValue;

                if (!emailOptions.EnableCertificateExpiryAlerts || string.IsNullOrWhiteSpace(emailOptions.AdminAlertRecipients))
                {
                    return Task.CompletedTask;
                }

                if (!this.smtpProvider.IsConfigured)
                {
                    return Task.CompletedTask;
                }

                var cert = this.httpSysConfigProvider.GetCertificate();

                if (cert == null)
                {
                    return Task.CompletedTask;
                }

                bool hasExpired = DateTime.Now > cert.NotAfter;
                int daysRemaining = (int)Math.Ceiling(cert.NotAfter.Subtract(DateTime.Now).TotalDays);

                if (daysRemaining > 30)
                {
                    return Task.CompletedTask;
                }

                string durationKey;

                if (daysRemaining <= 0)
                {
                    durationKey = "0";
                }
                else if (daysRemaining <= 1)
                {
                    durationKey = "1";
                }
                else if (daysRemaining <= 7)
                {
                    durationKey = "7";
                }
                else
                {
                    durationKey = "30";
                }

                string certKey = $"{cert.Thumbprint}-{durationKey}".ToLower();

                if (this.registryProvider.LastNotifiedCertificateKey == certKey)
                {
                    return Task.CompletedTask;
                }

                this.registryProvider.LastNotifiedCertificateKey = certKey;

                this.Send(cert, emailOptions.AdminAlertRecipients, hasExpired, daysRemaining);
            }
            catch (Exception ex)
            {
                logger.LogWarning(EventIDs.CertificateExpiryCheckJobFailed, ex, "Certificate expiry check failed");
            }

            return Task.CompletedTask;
        }

        private void Send(X509Certificate2 cert, string recipients, bool hasExpired, int daysRemaining)
        {

            string body = EmbeddedResourceProvider.GetResourceString("CertificateExpiringEmail.html", "EmbeddedResources.Templates");
            string subject = hasExpired ? "TLS Certificate has expired" : "TLS Certificate is expiring soon";

            Dictionary<string, string> tokens = new Dictionary<string, string>()
            {
                { "{thumbprint}", cert.Thumbprint},
                { "{notBefore}" , cert.NotBefore.ToString()},
                { "{notAfter}" , cert.NotAfter.ToString()},
                { "{subject}" , cert.Subject},
                { "{daysToExpiry}" ,hasExpired ? "0" : daysRemaining.ToString()},
                { "{serialNumber}" , cert.SerialNumber},
                { "{expiryNotice}" , hasExpired ? "The Access Manager TLS certificate has expired" : $"The Access Manager TLS certificate expires in {daysRemaining} day{(daysRemaining == 1 ? "" :"s")}"},
            };

            body = TokenReplacer.ReplaceAsHtml(tokens, body);
            subject = TokenReplacer.ReplaceAsPlainText(tokens, subject);

            this.smtpProvider.SendEmail(recipients, subject, body);
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
