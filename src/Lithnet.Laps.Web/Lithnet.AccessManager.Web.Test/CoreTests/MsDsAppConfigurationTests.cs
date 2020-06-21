using System;
using System.Linq;
using Moq;
using NLog.LayoutRenderers;
using NUnit.Framework;

namespace Lithnet.AccessManager.Test
{
    public class MsDsAppConfigurationTests
    {
        private Mock<NLog.ILogger> dummyLogger;

        private ActiveDirectory directory;

        private MsDsAppConfigurationProvider provider;

        [SetUp()]
        public void TestInitialize()
        {
            dummyLogger = new Mock<NLog.ILogger>();
            directory = new ActiveDirectory(Mock.Of<Microsoft.Extensions.Logging.ILogger<ActiveDirectory>>());
            provider = new MsDsAppConfigurationProvider();
        }

        [TestCase("IDMDEV1\\PC1")]
        public void CreateAppData(string computerName)
        {
            ActiveDirectory directory = new ActiveDirectory(Mock.Of<Microsoft.Extensions.Logging.ILogger<ActiveDirectory>>());
            IComputer computer = directory.GetComputer(computerName);

            if (provider.TryGetAppData(computer, out IAppData appData))
            {
                provider.DeleteAppData(appData);
            }

            var lamSettings = provider.Create(computer);

            var de = lamSettings.GetDirectoryEntry();
            CollectionAssert.Contains(de.GetPropertyStrings("objectClass"), MsDsAppConfigurationProvider.ObjectClass);
            Assert.AreEqual(MsDsAppConfigurationProvider.AttrApplicationName, de.GetPropertyString("applicationName"));
            Assert.AreEqual(MsDsAppConfigurationProvider.AttrCommonName, de.GetPropertyString("cn"));
            Assert.AreEqual(MsDsAppConfigurationProvider.AttrDescription, de.GetPropertyString("description"));
            Assert.AreEqual(de.Parent.Path, computer.GetDirectoryEntry().Path);
        }

        [TestCase("IDMDEV1\\PC1")]
        public void DeleteAppData(string computerName)
        {
            ActiveDirectory directory = new ActiveDirectory(Mock.Of<Microsoft.Extensions.Logging.ILogger<ActiveDirectory>>());
            IComputer computer = directory.GetComputer(computerName);

            if (!provider.TryGetAppData(computer, out IAppData appData))
            {
                appData = provider.Create(computer);
            }

            provider.DeleteAppData(appData);

            Assert.IsFalse(provider.TryGetAppData(computer, out _));
            Assert.IsTrue(directory.TryGetComputer(computerName, out _));
        }

        [TestCase("IDMDEV1\\PC1", "IDMDEV1\\JIT-PC1")]
        public void SetJitGroup(string computerName, string jitGroupName)
        {
            IComputer computer = directory.GetComputer(computerName);

            if (!provider.TryGetAppData(computer, out IAppData appData))
            {
                appData = provider.Create(computer);
            }

            appData.UpdateJitGroup(null);
            Assert.IsNull(appData.JitGroupReference);

            IGroup group = directory.GetGroup(jitGroupName);
            appData.UpdateJitGroup(group);

            Assert.AreEqual(group.DistinguishedName, appData.JitGroupReference);
        }

        [TestCase("IDMDEV1\\PC1")]
        public void SetFirstPassword(string computerName)
        {
            IComputer computer = directory.GetComputer(computerName);

            if (!provider.TryGetAppData(computer, out IAppData appData))
            {
                appData = provider.Create(computer);
            }

            appData.ClearPasswordHistory();
            CollectionAssert.IsEmpty(appData.PasswordHistory);

            DateTime created = DateTime.UtcNow.Trim(TimeSpan.TicksPerSecond);
            DateTime expired = DateTime.UtcNow.AddDays(-5).Trim(TimeSpan.TicksPerSecond);

            string password = "this is my data";

            appData.UpdateCurrentPassword(password, created, expired, 0);

            Assert.AreEqual(1, appData.PasswordHistory.Count);

            var returnedPphi = appData.PasswordHistory.First();

            Assert.AreEqual(created, returnedPphi.Created);
            Assert.AreEqual(null, returnedPphi.Retired);
            Assert.AreEqual(password, returnedPphi.EncryptedData);
            Assert.AreEqual(expired, appData.PasswordExpiry);
            Assert.AreEqual(password, appData.CurrentPassword.EncryptedData);
        }

        [TestCase("IDMDEV1\\PC1")]
        public void Create(string computerName)
        {
            IComputer computer = directory.GetComputer(computerName);

            if (!provider.TryGetAppData(computer, out IAppData appData))
            {
                appData = provider.Create(computer);
            }

            DateTime rotationInstant = DateTime.UtcNow;
            DateTime expiryDate = DateTime.UtcNow.AddDays(30);

            string newPassword = Guid.NewGuid().ToString();

            EncryptionProvider encryptionProvider = new EncryptionProvider();
            CertificateResolver certificateResolver = new CertificateResolver();

            appData.UpdateCurrentPassword(
                   encryptionProvider.Encrypt(
                       certificateResolver.GetEncryptionCertificate(
                          TestConstants.EncryptionCertificateThumbprint ),
                       newPassword),
                   rotationInstant,
                   expiryDate,
                   365);
        }

        [TestCase("IDMDEV1\\PC1")]
        public void AddToPasswordHistory(string computerName)
        {
            IComputer computer = directory.GetComputer(computerName);

            if (!provider.TryGetAppData(computer, out IAppData appData))
            {
                appData = provider.Create(computer);
            }

            appData.ClearPasswordHistory();
            CollectionAssert.IsEmpty(appData.PasswordHistory);

            DateTime firstCreated = DateTime.UtcNow.Trim(TimeSpan.TicksPerSecond);
            DateTime firstExpiry = DateTime.UtcNow.AddDays(-3).Trim(TimeSpan.TicksPerSecond);
            string firstPassword = "first password";

            appData.UpdateCurrentPassword(firstPassword, firstCreated, firstExpiry, 0);
            Assert.AreEqual(1, appData.PasswordHistory.Count);
            var firstHistoryItem = appData.PasswordHistory.First();
            Assert.AreEqual(firstCreated, firstHistoryItem.Created);
            Assert.AreEqual(null, firstHistoryItem.Retired);
            Assert.AreEqual(firstPassword, firstHistoryItem.EncryptedData);
            Assert.AreEqual(firstExpiry, appData.PasswordExpiry);
            Assert.AreEqual(firstPassword, appData.CurrentPassword.EncryptedData);

            DateTime secondCreated = DateTime.UtcNow.AddDays(2).Trim(TimeSpan.TicksPerSecond);
            DateTime secondExpiry = DateTime.UtcNow.AddDays(-5).Trim(TimeSpan.TicksPerSecond);
            string secondPassword = "second password";

            appData.UpdateCurrentPassword(secondPassword, secondCreated, secondExpiry, 30);

            Assert.AreEqual(2, appData.PasswordHistory.Count);
            firstHistoryItem = appData.PasswordHistory.Single(t => t.EncryptedData == firstPassword);
            Assert.AreEqual(firstCreated, firstHistoryItem.Created);
            Assert.AreEqual(secondCreated, firstHistoryItem.Retired);
            Assert.AreEqual(firstPassword, firstHistoryItem.EncryptedData);
            Assert.AreEqual(secondExpiry, appData.PasswordExpiry);
            Assert.AreEqual(secondPassword, appData.CurrentPassword.EncryptedData);

            var secondHistoryItem = appData.PasswordHistory.Single(t => t.EncryptedData == secondPassword);
            Assert.AreEqual(secondCreated, secondHistoryItem.Created);
            Assert.AreEqual(null, secondHistoryItem.Retired);
            Assert.AreEqual(secondPassword, secondHistoryItem.EncryptedData);
            Assert.AreEqual(secondExpiry, appData.PasswordExpiry);
            Assert.AreEqual(secondPassword, appData.CurrentPassword.EncryptedData);
        }

        [TestCase("IDMDEV1\\PC1")]
        public void AgeOutPasswordHistory(string computerName)
        {
            IComputer computer = directory.GetComputer(computerName);

            if (!provider.TryGetAppData(computer, out IAppData appData))
            {
                appData = provider.Create(computer);
            }

            appData.ClearPasswordHistory();
            CollectionAssert.IsEmpty(appData.PasswordHistory);

            DateTime firstCreated = DateTime.UtcNow.AddDays(-2).Trim(TimeSpan.TicksPerSecond);
            DateTime firstExpiry = DateTime.UtcNow.AddDays(-5).Trim(TimeSpan.TicksPerSecond);
            string firstPassword = "first password";

            appData.UpdateCurrentPassword(firstPassword, firstCreated, firstExpiry, 0);
            Assert.AreEqual(1, appData.PasswordHistory.Count);
            var firstHistoryItem = appData.PasswordHistory.First();
            Assert.AreEqual(firstCreated, firstHistoryItem.Created);
            Assert.AreEqual(null, firstHistoryItem.Retired);
            Assert.AreEqual(firstPassword, firstHistoryItem.EncryptedData);
            Assert.AreEqual(firstExpiry, appData.PasswordExpiry);
            Assert.AreEqual(firstPassword, appData.CurrentPassword.EncryptedData);

            DateTime secondCreated = DateTime.UtcNow.AddDays(-4).Trim(TimeSpan.TicksPerSecond);
            DateTime secondExpiry = DateTime.UtcNow.AddDays(-7).Trim(TimeSpan.TicksPerSecond);
            string secondPassword = "second password";

            appData.UpdateCurrentPassword(secondPassword, secondCreated, secondExpiry, 1);

            Assert.AreEqual(1, appData.PasswordHistory.Count);
            var secondHistoryItem = appData.PasswordHistory.Single(t => t.EncryptedData == secondPassword);
            Assert.AreEqual(secondCreated, secondHistoryItem.Created);
            Assert.AreEqual(null, secondHistoryItem.Retired);
            Assert.AreEqual(secondPassword, secondHistoryItem.EncryptedData);
            Assert.AreEqual(secondExpiry, appData.PasswordExpiry);
            Assert.AreEqual(secondPassword, appData.CurrentPassword.EncryptedData);
        }
    }
}