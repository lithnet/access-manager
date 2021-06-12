using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Linq;
using Microsoft.Extensions.Logging;
using C = Lithnet.AccessManager.Test.TestEnvironmentConstants;
using NUnit.Framework;
using Moq;

namespace Lithnet.AccessManager.PowerShell.Test
{
    public class GetLithnetLocalAdminPasswordTests
    {
        private IDirectory directory;

        private IDiscoveryServices discoveryServices;

        private LithnetAdminPasswordProvider provider;

        private IEncryptionProvider encryptionProvider;

        private ICertificateProvider certificateProvider;

        [SetUp()]
        public void TestInitialize()
        {
            encryptionProvider = new EncryptionProvider();
            this.discoveryServices = new DiscoveryServices(Mock.Of<ILogger<DiscoveryServices>>());
            directory = new ActiveDirectory(discoveryServices);
            certificateProvider = new CertificateProvider(Mock.Of<ILogger<CertificateProvider>>(), discoveryServices);
            provider = new LithnetAdminPasswordProvider(Mock.Of<ILogger<LithnetAdminPasswordProvider>>(), encryptionProvider, certificateProvider);
        }

        [TestCase(C.DEV_PC1)]
        [TestCase(C.SUBDEV_PC1)]
        [TestCase(C.EXTDEV_PC1)]
        [TestCase(C.DEV_PC2)]
        [TestCase(C.SUBDEV_PC2)]
        [TestCase(C.EXTDEV_PC2)]
        public void AddToPasswordHistory(string computerName)
        {
            IComputer computer = directory.GetComputer(computerName);
            provider.ClearPassword(computer);
            provider.ClearPasswordHistory(computer);
            CollectionAssert.IsEmpty(provider.GetPasswordHistory(computer));

            DateTime firstCreated = DateTime.UtcNow.Trim(TimeSpan.TicksPerSecond);
            DateTime firstExpiry = DateTime.UtcNow.AddDays(-3).Trim(TimeSpan.TicksPerSecond);
            string firstPassword = Guid.NewGuid().ToString();

            provider.UpdateCurrentPassword(computer, firstPassword, firstCreated, firstExpiry, 0, PasswordAttributeBehaviour.Ignore);

            DateTime secondCreated = DateTime.UtcNow.AddDays(2).Trim(TimeSpan.TicksPerSecond);
            DateTime secondExpiry = DateTime.UtcNow.AddDays(-5).Trim(TimeSpan.TicksPerSecond);
            string secondPassword = Guid.NewGuid().ToString();

            provider.UpdateCurrentPassword(computer, secondPassword, secondCreated, secondExpiry, 30, PasswordAttributeBehaviour.Ignore);

            System.Management.Automation.PowerShell ps = System.Management.Automation.PowerShell.Create();
            ps.AddCommand(new CmdletInfo("Get-LithnetLocalAdminPasswordHistory", typeof(GetLocalAdminPasswordHistory)));
            ps.AddParameter("ComputerName", computerName);
            var output = ps.Invoke();

            Assert.AreEqual(1, output.Count);

            var passwords = output.Select(t => t.Properties["Password"].Value as string).ToList();

            CollectionAssert.AreEquivalent(new[] { firstPassword }, passwords);
        }

        [TestCase(C.DEV_PC1)]
        [TestCase(C.SUBDEV_PC1)]
        [TestCase(C.EXTDEV_PC1)]
        [TestCase(C.DEV_PC2)]
        [TestCase(C.SUBDEV_PC2)]
        [TestCase(C.EXTDEV_PC2)]
        public void GetLocalAdminPassword(string computerName)
        {
            IComputer computer = directory.GetComputer(computerName);

            this.provider.ClearPassword(computer);
            this.provider.ClearPasswordHistory(computer);

            CollectionAssert.IsEmpty(this.provider.GetPasswordHistory(computer));
            Assert.IsNull(this.provider.GetCurrentPassword(computer, null));

            DateTime created = DateTime.UtcNow.Trim(TimeSpan.TicksPerSecond);
            DateTime expired = DateTime.UtcNow.AddDays(-5).Trim(TimeSpan.TicksPerSecond);
            string password = Guid.NewGuid().ToString();

            this.provider.UpdateCurrentPassword(computer, password, created, expired, 0, PasswordAttributeBehaviour.Ignore);

            System.Management.Automation.PowerShell ps = System.Management.Automation.PowerShell.Create();
            ps.AddCommand(new CmdletInfo("Get-LithnetLocalAdminPassword", typeof(GetLocalAdminPassword)));
            ps.AddParameter("ComputerName", computerName);
            var output = ps.Invoke();

            Assert.AreEqual(1, output.Count);
            var result = output[0];

            Assert.AreEqual(password, result.Properties["Password"].Value);
        }
    }
}
