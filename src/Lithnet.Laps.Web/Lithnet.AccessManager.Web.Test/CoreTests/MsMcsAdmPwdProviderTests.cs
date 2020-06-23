using System;
using System.Linq;
using Moq;
using NUnit.Framework;

namespace Lithnet.AccessManager.Test
{
    public class MsMcsAdmPwdProviderTests
    {
        private ActiveDirectory directory;

        private MsMcsAdmPwdProvider provider;

        [SetUp()]
        public void TestInitialize()
        {
            directory = new ActiveDirectory(Mock.Of<Microsoft.Extensions.Logging.ILogger<ActiveDirectory>>());
            provider = new MsMcsAdmPwdProvider(Mock.Of<Microsoft.Extensions.Logging.ILogger<MsMcsAdmPwdProvider>>());
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