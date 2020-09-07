using System;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Lithnet.AccessManager.Test
{
    public class MsMcsAdmPwdProviderTests
    {
        private IDirectory directory;

        private IDiscoveryServices discoveryServices;

        private MsMcsAdmPwdProvider provider;

        [SetUp()]
        public void TestInitialize()
        {
            this.discoveryServices = new DiscoveryServices(Mock.Of<ILogger<DiscoveryServices>>());
            directory = new ActiveDirectory(discoveryServices);
            provider = new MsMcsAdmPwdProvider(Mock.Of<ILogger<MsMcsAdmPwdProvider>>());
        }

        [TestCase("IDMDEV1\\PC1")]
        public void UpdatePassword(string computerName)
        {
            var computer = this.directory.GetComputer(computerName);
            string password = Guid.NewGuid().ToString();
            DateTime expiry = DateTime.UtcNow;

            provider.SetPassword(computer, password, expiry);

            var result = provider.GetPassword(computer, null);

            Assert.AreEqual(password, result.Password);
            Assert.AreEqual(expiry, result.ExpiryDate);
        }

        [TestCase("IDMDEV1\\PC1")]
        public void GetPasswordWithUpdatedExpiry(string computerName)
        {
            var computer = this.directory.GetComputer(computerName);
            string password = Guid.NewGuid().ToString();
            DateTime expiry1 = DateTime.UtcNow;
            DateTime expiry2 = DateTime.UtcNow.AddDays(5);

            provider.SetPassword(computer, password, expiry1);

            var result = provider.GetPassword(computer, expiry2);

            Assert.AreEqual(password, result.Password);
            Assert.AreEqual(expiry2, result.ExpiryDate);
        }

        [TestCase("IDMDEV1\\PC1")]
        public void GetExpiry(string computerName)
        {
            var computer = this.directory.GetComputer(computerName);
            string password = Guid.NewGuid().ToString();
            DateTime expiry = DateTime.UtcNow;

            provider.SetPassword(computer, password, expiry);

            var result = provider.GetPassword(computer, null);

            Assert.AreEqual(password, result.Password);
            Assert.AreEqual(expiry, result.ExpiryDate);

            Assert.AreEqual(expiry, provider.GetExpiry(computer));
        }
    }
}