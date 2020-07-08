using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Castle.Core.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Lithnet.AccessManager.Test
{
    public class CertificateResolverTests
    {
        private Mock<IAppPathProvider> env;

        private CertificateProvider provider;

        private ActiveDirectory directory;

        [SetUp()]
        public void TestInitialize()
        {
            this.env = new Mock<IAppPathProvider>();
            this.env.SetupGet(t => t.AppPath).Returns(Environment.CurrentDirectory);
            this.directory = new ActiveDirectory();
            provider = new CertificateProvider(Mock.Of<ILogger<CertificateProvider>>(), directory, env.Object);
        }

        [TestCase(StoreLocation.CurrentUser)]
        [TestCase(StoreLocation.LocalMachine)]
        public void GetEncryptionCertificateFromStore(StoreLocation location)
        {
            X509Store store = new X509Store(StoreName.My, location, OpenFlags.ReadOnly);

            foreach (var cert in store.Certificates)
            {
                Assert.AreEqual(cert, provider.FindCertificate(false, cert.Thumbprint));
            }

            Assert.Throws<CertificateNotFoundException>(() => provider.FindCertificate(false, "ABCDE"));
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
                    Assert.AreEqual(cert, provider.FindCertificate(true, cert.Thumbprint));
                }
                else
                {
                    Assert.Throws<CertificateValidationException>(() => provider.FindCertificate(true, cert.Thumbprint));
                }
            }

            Assert.Throws<CertificateNotFoundException>(() => provider.FindCertificate(true, "ABCDE"));
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
            Assert.IsTrue(provider.TryGetCertificateFromDirectory(out X509Certificate2 cert));
            Assert.IsNotNull(cert);
        }
    }
}
