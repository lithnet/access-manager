using System;
using System.Diagnostics;
using System.DirectoryServices.ActiveDirectory;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Lithnet.AccessManager.Test
{
    public class CertificateProviderTests
    {
        private Mock<IAppPathProvider> env;

        private CertificateProvider provider;

        private IDirectory directory;

        private IDiscoveryServices discoveryServices;

        [SetUp()]
        public void TestInitialize()
        {
            this.env = new Mock<IAppPathProvider>();
            this.env.SetupGet(t => t.AppPath).Returns(Environment.CurrentDirectory);
            this.discoveryServices = new DiscoveryServices();
            this.directory = new ActiveDirectory(this.discoveryServices);
            provider = new CertificateProvider(Mock.Of<ILogger<CertificateProvider>>(), directory, env.Object, discoveryServices);
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
            var store = this.provider.OpenServiceStore(Constants.ServiceName, OpenFlags.ReadWrite);

            foreach (var cert in store.Certificates)
            {
                if (cert.HasPrivateKey)
                {
                    Assert.AreEqual(cert, provider.FindDecryptionCertificate(cert.Thumbprint, Constants.ServiceName));
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
                var store = this.provider.OpenServiceStore(Constants.ServiceName, OpenFlags.ReadWrite);
                cert = this.provider.CreateSelfSignedCert(TestContext.CurrentContext.Random.GetString(10));
                store.Add(cert);
                store.Close();

                Assert.AreEqual(cert, provider.FindDecryptionCertificate(cert.Thumbprint, Constants.ServiceName));
            }
            finally
            {
                if (cert != null)
                {
                    var store = this.provider.OpenServiceStore(Constants.ServiceName, OpenFlags.ReadWrite);
                    store.Remove(cert);
                    store.Close();
                }
            }

        }

        [TestCase("TestFiles\\DigiCertGlobalRootG3.crt")]
        public void GetCertificateFromPartialPath(string path)
        {
            Assert.IsTrue(provider.TryGetCertificateFromPath(path, out X509Certificate2 cert));
            Assert.IsNotNull(cert);
        }

        [TestCase("TestFiles\\DigiCertGlobalRootG3.crt")]
        public void GetCertificateFromFullPath(string path)
        {
            path = Path.Combine(Environment.CurrentDirectory, path);
            Assert.IsTrue(provider.TryGetCertificateFromPath(path, out X509Certificate2 cert));
            Assert.IsNotNull(cert);
        }

        [TestCase("https://cacerts.digicert.com/DigiCertGlobalRootG3.crt")]
        public void GetCertificateFromUrl(string url)
        {
            Uri uri = new Uri(url);
            Assert.IsTrue(provider.TryGetCertificateFromUrl(uri, out X509Certificate2 cert));
            Assert.IsNotNull(cert);
        }

        [TestCase("https://cacerts.digicert.com/DigiCertGlobalRootG3.crt")]
        public void GetCertificateFromPathUrl(string url)
        {
            Assert.IsTrue(provider.TryGetCertificateFromPath(url, out X509Certificate2 cert));
            Assert.IsNotNull(cert);
        }

        [Test]
        public void GetCertificateFromDirectory()
        {
            Assert.IsTrue(provider.TryGetCertificateFromDirectory(out X509Certificate2 cert, Domain.GetComputerDomain().Name));
            Assert.IsNotNull(cert);
        }
    }
}
