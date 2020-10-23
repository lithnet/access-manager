using System.DirectoryServices.ActiveDirectory;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Lithnet.AccessManager.Test
{
    public class CertificateProviderTests
    {
        private CertificateProvider provider;

        private IDiscoveryServices discoveryServices;

        [SetUp()]
        public void TestInitialize()
        {
            this.discoveryServices = new DiscoveryServices(Mock.Of<ILogger<DiscoveryServices>>());
            provider = new CertificateProvider(Mock.Of<ILogger<CertificateProvider>>(), discoveryServices);
        }

        [TestCase(StoreLocation.CurrentUser)]
        [TestCase(StoreLocation.LocalMachine)]
        public void GetEncryptionCertificateFromStore(StoreLocation location)
        {
            X509Store store = new X509Store(StoreName.My, location, OpenFlags.ReadOnly);

            foreach (var cert in store.Certificates)
            {
                Assert.AreEqual(cert, provider.FindEncryptionCertificate(cert.Thumbprint));
            }

            Assert.Throws<CertificateNotFoundException>(() => provider.FindEncryptionCertificate("ABCDE"));
        }

        [TestCase(StoreLocation.CurrentUser)]
        [TestCase(StoreLocation.LocalMachine)]
        public void GetDecryptionCertificateFromStore(StoreLocation location)
        {
            X509Store store = new X509Store(StoreName.My, location, OpenFlags.ReadOnly);

            foreach (var cert in store.Certificates)
            {
                if (cert.HasPrivateKey)
                {
                    Assert.AreEqual(cert, provider.FindDecryptionCertificate(cert.Thumbprint));
                }
                else
                {
                    Assert.Throws<CertificateValidationException>(() => provider.FindDecryptionCertificate(cert.Thumbprint));
                }
            }

            Assert.Throws<CertificateNotFoundException>(() => provider.FindDecryptionCertificate("ABCDE"));
        }

        [Test]
        public void GetDecryptionCertificateFromServiceStore()
        {
            var store = X509ServiceStoreHelper.Open(Constants.ServiceName, OpenFlags.ReadWrite);

            foreach (var cert in store.Certificates)
            {
                if (cert.HasPrivateKey)
                {
                    Assert.AreEqual(cert, provider.FindDecryptionCertificate(cert.Thumbprint));
                }
                else
                {
                    Assert.Throws<CertificateValidationException>(() => provider.FindDecryptionCertificate(cert.Thumbprint));
                }
            }

            Assert.Throws<CertificateNotFoundException>(() => provider.FindDecryptionCertificate("ABCDE"));
        }


        [Test]
        public void AddDecryptionCertificateToServiceStore()
        {
            X509Certificate2 cert = null;
            try
            {
                var store = X509ServiceStoreHelper.Open(Constants.ServiceName, OpenFlags.ReadWrite);
                cert = this.provider.CreateSelfSignedCert(TestContext.CurrentContext.Random.GetString(10));
                store.Add(cert);
                store.Close();

                Assert.AreEqual(cert, provider.FindDecryptionCertificate(cert.Thumbprint));
            }
            finally
            {
                if (cert != null)
                {
                    var store = X509ServiceStoreHelper.Open(Constants.ServiceName, OpenFlags.ReadWrite);
                    store.Remove(cert);
                    store.Close();
                }
            }

        }

        [Test]
        public void GetCertificateFromDirectory()
        {
            Assert.IsTrue(provider.TryGetCertificateFromDirectory(out X509Certificate2 cert, Domain.GetComputerDomain().Name));
            Assert.IsNotNull(cert);
        }
    }
}
