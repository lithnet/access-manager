using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using Moq;
using Microsoft.Extensions.Logging;
using Lithnet.AccessManager.Server.Configuration;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;
using Quartz;

namespace Lithnet.AccessManager.Server.Test
{
    public class NewVersionCheckJobTest
    {
        private Mock<ISmtpProvider> smtpProvider;
        private ILogger<NewVersionCheckJob> logger;
        private Mock<IOptionsMonitor<AdminNotificationOptions>> adminNotificationOptions;
        private Mock<IRegistryProvider> registryProvider;

        [SetUp]
        public void Setup()
        {
            this.smtpProvider = new Mock<ISmtpProvider>();
            this.smtpProvider.SetupGet(x => x.IsConfigured).Returns(true);

            this.logger = Global.LogFactory.CreateLogger<NewVersionCheckJob>();

            this.adminNotificationOptions = new Mock<IOptionsMonitor<AdminNotificationOptions>>();
            this.adminNotificationOptions.SetupGet(t => t.CurrentValue).Returns(new AdminNotificationOptions() { AdminAlertRecipients = "test@test.com", EnableNewVersionAlerts = true, });

            this.registryProvider = new Mock<IRegistryProvider>();
            this.registryProvider.SetupGet(t => t.LastNotifiedVersion).Returns((string)null);
        }

        [Test]
        public async Task TestUpgradeAvailableAsync()
        {
            Mock<IApplicationUpgradeProvider> appUpgradeProvider = new Mock<IApplicationUpgradeProvider>();
            appUpgradeProvider.Setup(t => t.GetVersionInfo()).Returns(Task.FromResult(new AppVersionInfo()
            {
                AvailableVersion = new Version("2.0.0.0"),
                CurrentVersion = new Version("1.0.0.0"),
                Status = VersionInfoStatus.UpdateAvailable
            }));

            var job = new NewVersionCheckJob(appUpgradeProvider.Object, this.logger, smtpProvider.Object, adminNotificationOptions.Object, registryProvider.Object);

            await job.Execute(Mock.Of<IJobExecutionContext>());
            appUpgradeProvider.Verify(t => t.GetVersionInfo());
            smtpProvider.VerifyGet(t => t.IsConfigured);
            smtpProvider.Verify(t => t.SendEmail(It.Is<string>(x => x == "test@test.com"), It.IsAny<string>(), It.IsAny<string>()));

            registryProvider.VerifySet(t => t.LastNotifiedVersion = "2.0.0.0");
            
        }

        [Test]
        public async Task TestUpgradeNotAvailableAsync()
        {
            Mock<IApplicationUpgradeProvider> appUpgradeProvider = new Mock<IApplicationUpgradeProvider>();
            appUpgradeProvider.Setup(t => t.GetVersionInfo()).Returns(Task.FromResult(new AppVersionInfo()
            {
                AvailableVersion = new Version("1.0.0.0"),
                CurrentVersion = new Version("1.0.0.0"),
                Status = VersionInfoStatus.Current
            }));

            var job = new NewVersionCheckJob(appUpgradeProvider.Object, this.logger, smtpProvider.Object, adminNotificationOptions.Object, registryProvider.Object);

            await job.Execute(Mock.Of<IJobExecutionContext>());
            appUpgradeProvider.Verify(t => t.GetVersionInfo());
            smtpProvider.VerifyGet(t => t.IsConfigured);
            smtpProvider.Verify(t => t.SendEmail(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);

            registryProvider.VerifySet(t=> t.LastNotifiedVersion = It.IsAny<string>(), Times.Never);
        }
    }
}
