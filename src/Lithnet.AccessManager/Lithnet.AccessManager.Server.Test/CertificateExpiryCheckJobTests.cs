using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using Moq;
using Microsoft.Extensions.Logging;
using Lithnet.AccessManager.Server.Configuration;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;
using Quartz;
using System.Security.Cryptography;
using C = Lithnet.AccessManager.Test.TestEnvironmentConstants;

namespace Lithnet.AccessManager.Server.Test
{
    public class CertificateExpiryCheckJobTests
    {
        private Mock<ISmtpProvider> smtpProvider;
        private ILogger<CertificateExpiryCheckJob> logger;
        private Mock<IOptionsMonitor<AdminNotificationOptions>> adminNotificationOptions;
        private Mock<IRegistryProvider> registryProvider;


        [SetUp]
        public void Setup()
        {
            this.smtpProvider = new Mock<ISmtpProvider>();
            this.smtpProvider.SetupGet(x => x.IsConfigured).Returns(true);

            this.logger = Global.LogFactory.CreateLogger<CertificateExpiryCheckJob>();

            this.adminNotificationOptions = new Mock<IOptionsMonitor<AdminNotificationOptions>>();
            this.adminNotificationOptions.SetupGet(t => t.CurrentValue).Returns(new AdminNotificationOptions() { AdminAlertRecipients = "test@test.com", EnableCertificateExpiryAlerts = true, });

            this.registryProvider = new Mock<IRegistryProvider>();
            this.registryProvider.SetupGet(t => t.LastNotifiedVersion).Returns((string)null);
        }

        [Test]
        public async Task TestNotificationSentWhenCertificateExpiresIn30DaysAsync()
        {
            Mock<IHttpSysConfigurationProvider> httpSysProvider = new Mock<IHttpSysConfigurationProvider>();
            var cert = this.CreateSelfSignedCert(DateTime.Now.Subtract(TimeSpan.FromDays(90)), DateTime.Now.Add(TimeSpan.FromDays(30)));

            httpSysProvider.Setup(t => t.GetCertificate()).Returns(cert);

            var job = new CertificateExpiryCheckJob(httpSysProvider.Object, this.logger, registryProvider.Object, smtpProvider.Object, adminNotificationOptions.Object);

            await job.Execute(Mock.Of<IJobExecutionContext>());

            httpSysProvider.Verify(t => t.GetCertificate());
            smtpProvider.VerifyGet(t => t.IsConfigured);
            smtpProvider.Verify(t => t.SendEmail(It.Is<string>(x => x == "test@test.com"), It.IsAny<string>(), It.IsAny<string>()));
            registryProvider.VerifySet(t => t.LastNotifiedCertificateKey = $"{cert.Thumbprint}-30".ToLowerInvariant());
        }

        [Test]
        public async Task TestNotificationSentWhenCertificateExpiresIn7DaysAsync()
        {
            Mock<IHttpSysConfigurationProvider> httpSysProvider = new Mock<IHttpSysConfigurationProvider>();
            var cert = this.CreateSelfSignedCert(DateTime.Now.Subtract(TimeSpan.FromDays(90)), DateTime.Now.Add(TimeSpan.FromDays(7)));

            httpSysProvider.Setup(t => t.GetCertificate()).Returns(cert);

            var job = new CertificateExpiryCheckJob(httpSysProvider.Object, this.logger, registryProvider.Object, smtpProvider.Object, adminNotificationOptions.Object);

            await job.Execute(Mock.Of<IJobExecutionContext>());

            httpSysProvider.Verify(t => t.GetCertificate());
            smtpProvider.VerifyGet(t => t.IsConfigured);
            smtpProvider.Verify(t => t.SendEmail(It.Is<string>(x => x == "test@test.com"), It.IsAny<string>(), It.IsAny<string>()));
            registryProvider.VerifySet(t => t.LastNotifiedCertificateKey = $"{cert.Thumbprint}-7".ToLowerInvariant());
        }

        [Test]
        public async Task TestNotificationSentWhenCertificateExpiresIn1DaysAsync()
        {
            Mock<IHttpSysConfigurationProvider> httpSysProvider = new Mock<IHttpSysConfigurationProvider>();
            var cert = this.CreateSelfSignedCert(DateTime.Now.Subtract(TimeSpan.FromDays(90)), DateTime.Now.Add(TimeSpan.FromHours(23)));

            httpSysProvider.Setup(t => t.GetCertificate()).Returns(cert);

            var job = new CertificateExpiryCheckJob(httpSysProvider.Object, this.logger, registryProvider.Object, smtpProvider.Object, adminNotificationOptions.Object);

            await job.Execute(Mock.Of<IJobExecutionContext>());

            httpSysProvider.Verify(t => t.GetCertificate());
            smtpProvider.VerifyGet(t => t.IsConfigured);
            smtpProvider.Verify(t => t.SendEmail(It.Is<string>(x => x == "test@test.com"), It.IsAny<string>(), It.IsAny<string>()));
            registryProvider.VerifySet(t => t.LastNotifiedCertificateKey = $"{cert.Thumbprint}-1".ToLowerInvariant());
        }

        [Test]
        public async Task TestNotificationSentWhenCertificateExpiresIn0DaysAsync()
        {
            Mock<IHttpSysConfigurationProvider> httpSysProvider = new Mock<IHttpSysConfigurationProvider>();
            var cert = this.CreateSelfSignedCert(DateTime.Now.Subtract(TimeSpan.FromDays(90)), DateTime.Now.Add(TimeSpan.FromDays(-1)));

            httpSysProvider.Setup(t => t.GetCertificate()).Returns(cert);

            var job = new CertificateExpiryCheckJob(httpSysProvider.Object, this.logger, registryProvider.Object, smtpProvider.Object, adminNotificationOptions.Object);

            await job.Execute(Mock.Of<IJobExecutionContext>());

            httpSysProvider.Verify(t => t.GetCertificate());
            smtpProvider.VerifyGet(t => t.IsConfigured);
            smtpProvider.Verify(t => t.SendEmail(It.Is<string>(x => x == "test@test.com"), It.IsAny<string>(), It.IsAny<string>()));
            registryProvider.VerifySet(t => t.LastNotifiedCertificateKey = $"{cert.Thumbprint}-0".ToLowerInvariant());
        }


        [Test]
        public async Task TestNotificationNotSentWhenAlreadyNotifiedFor30Days()
        {
            Mock<IHttpSysConfigurationProvider> httpSysProvider = new Mock<IHttpSysConfigurationProvider>();
            var cert = this.CreateSelfSignedCert(DateTime.Now.Subtract(TimeSpan.FromDays(90)), DateTime.Now.Add(TimeSpan.FromDays(30)));

            httpSysProvider.Setup(t => t.GetCertificate()).Returns(cert);
            registryProvider.SetupGet(t => t.LastNotifiedCertificateKey).Returns($"{cert.Thumbprint}-30".ToLowerInvariant());

            var job = new CertificateExpiryCheckJob(httpSysProvider.Object, this.logger, registryProvider.Object, smtpProvider.Object, adminNotificationOptions.Object);

            await job.Execute(Mock.Of<IJobExecutionContext>());

            httpSysProvider.Verify(t => t.GetCertificate());
            smtpProvider.VerifyGet(t => t.IsConfigured);
            smtpProvider.Verify(t => t.SendEmail(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            registryProvider.VerifySet(t => t.LastNotifiedCertificateKey = It.IsAny<string>(), Times.Never);

        }

        [Test]
        public async Task TestNotificationNotSentWhenAlreadyNotifiedFor7Days()
        {
            Mock<IHttpSysConfigurationProvider> httpSysProvider = new Mock<IHttpSysConfigurationProvider>();
            var cert = this.CreateSelfSignedCert(DateTime.Now.Subtract(TimeSpan.FromDays(90)), DateTime.Now.Add(TimeSpan.FromDays(5)));

            httpSysProvider.Setup(t => t.GetCertificate()).Returns(cert);
            registryProvider.SetupGet(t => t.LastNotifiedCertificateKey).Returns($"{cert.Thumbprint}-7".ToLowerInvariant());

            var job = new CertificateExpiryCheckJob(httpSysProvider.Object, this.logger, registryProvider.Object, smtpProvider.Object, adminNotificationOptions.Object);

            await job.Execute(Mock.Of<IJobExecutionContext>());

            httpSysProvider.Verify(t => t.GetCertificate());
            smtpProvider.VerifyGet(t => t.IsConfigured);
            smtpProvider.Verify(t => t.SendEmail(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            registryProvider.VerifySet(t => t.LastNotifiedCertificateKey = It.IsAny<string>(), Times.Never);

        }

        [Test]
        public async Task TestNotificationNotSentWhenAlreadyNotifiedFor1Day()
        {
            Mock<IHttpSysConfigurationProvider> httpSysProvider = new Mock<IHttpSysConfigurationProvider>();
            var cert = this.CreateSelfSignedCert(DateTime.Now.Subtract(TimeSpan.FromDays(90)), DateTime.Now.Add(TimeSpan.FromHours(24)));

            httpSysProvider.Setup(t => t.GetCertificate()).Returns(cert);
            registryProvider.SetupGet(t => t.LastNotifiedCertificateKey).Returns($"{cert.Thumbprint}-1".ToLowerInvariant());

            var job = new CertificateExpiryCheckJob(httpSysProvider.Object, this.logger, registryProvider.Object, smtpProvider.Object, adminNotificationOptions.Object);

            await job.Execute(Mock.Of<IJobExecutionContext>());

            httpSysProvider.Verify(t => t.GetCertificate());
            smtpProvider.VerifyGet(t => t.IsConfigured);
            smtpProvider.Verify(t => t.SendEmail(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            registryProvider.VerifySet(t => t.LastNotifiedCertificateKey = It.IsAny<string>(), Times.Never);
        }

        [Test]
        public async Task TestNotificationNotSentWhenAlreadyNotifiedForExpired()
        {
            Mock<IHttpSysConfigurationProvider> httpSysProvider = new Mock<IHttpSysConfigurationProvider>();
            var cert = this.CreateSelfSignedCert(DateTime.Now.Subtract(TimeSpan.FromDays(90)), DateTime.Now.Add(TimeSpan.FromDays(-2)));

            httpSysProvider.Setup(t => t.GetCertificate()).Returns(cert);
            registryProvider.SetupGet(t => t.LastNotifiedCertificateKey).Returns($"{cert.Thumbprint}-0".ToLowerInvariant());

            var job = new CertificateExpiryCheckJob(httpSysProvider.Object, this.logger, registryProvider.Object, smtpProvider.Object, adminNotificationOptions.Object);

            await job.Execute(Mock.Of<IJobExecutionContext>());

            httpSysProvider.Verify(t => t.GetCertificate());
            smtpProvider.VerifyGet(t => t.IsConfigured);
            smtpProvider.Verify(t => t.SendEmail(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            registryProvider.VerifySet(t => t.LastNotifiedCertificateKey = It.IsAny<string>(), Times.Never);
        }

        [Test]
        public async Task TestNotificationNotSentWhenExpiryGreaterThan30Days()
        {
            Mock<IHttpSysConfigurationProvider> httpSysProvider = new Mock<IHttpSysConfigurationProvider>();
            var cert = this.CreateSelfSignedCert(DateTime.Now.Subtract(TimeSpan.FromDays(90)), DateTime.Now.Add(TimeSpan.FromDays(60)));

            httpSysProvider.Setup(t => t.GetCertificate()).Returns(cert);

            var job = new CertificateExpiryCheckJob(httpSysProvider.Object, this.logger, registryProvider.Object, smtpProvider.Object, adminNotificationOptions.Object);

            await job.Execute(Mock.Of<IJobExecutionContext>());
            httpSysProvider.Verify(t => t.GetCertificate());
            smtpProvider.VerifyGet(t => t.IsConfigured);
            smtpProvider.Verify(t => t.SendEmail(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            registryProvider.VerifySet(t => t.LastNotifiedCertificateKey = It.IsAny<string>(), Times.Never);
        }

        private X509Certificate2 CreateSelfSignedCert(DateTime notBefore, DateTime notAfter)
        {
            CertificateRequest request = new CertificateRequest($"CN=UnitTest,OU=Access Manager,O=Lithnet", RSA.Create(2048), HashAlgorithmName.SHA256, RSASignaturePadding.Pss);

            var enhancedKeyUsage = new OidCollection { CertificateProvider.ServerAuthenticationEku };
            request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(enhancedKeyUsage, critical: true));
            request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyEncipherment, true));
            request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));

            X509Certificate2 cert = request.CreateSelfSigned(notBefore, notAfter);
            return cert;
        }
    }
}
