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
        private Mock<IHostEnvironment> env;

        private CertificateResolver resolver;

        private ActiveDirectory directory;

        [SetUp()]
        public void TestInitialize()
        {
            this.env = new Mock<IHostEnvironment>();
            this.env.SetupGet(t => t.ContentRootPath).Returns(Environment.CurrentDirectory);
            this.directory = new ActiveDirectory();
            resolver = new CertificateResolver(Mock.Of<ILogger<CertificateResolver>>(), directory, env.Object);
        }

        [TestCase(StoreLocation.CurrentUser)]
        [TestCase(StoreLocation.LocalMachine)]
        public void GetEncryptionCertificateFromStore(StoreLocation location)
        {
            X509Store store = new X509Store(StoreName.My, location, OpenFlags.ReadOnly);

            foreach (var cert in store.Certificates)
            {
                Assert.AreEqual(cert, resolver.FindCertificate(false, cert.Thumbprint));
            }

            Assert.Throws<CertificateNotFoundException>(() => resolver.FindCertificate(false, "ABCDE"));
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
                    Assert.AreEqual(cert, resolver.FindCertificate(true, cert.Thumbprint));
                }
                else
                {
                    Assert.Throws<CertificateValidationException>(() => resolver.FindCertificate(true, cert.Thumbprint));
                }
            }

            Assert.Throws<CertificateNotFoundException>(() => resolver.FindCertificate(true, "ABCDE"));
        }

        [TestCase("TestFiles\\DigiCertGlobalRootG3.crt")]
        public void GetCertificateFromPartialPath(string path)
        {
            Assert.IsTrue(resolver.TryGetCertificateFromPath(path, out X509Certificate2 cert));
            Assert.IsNotNull(cert);
        }

        [TestCase("TestFiles\\DigiCertGlobalRootG3.crt")]
        public void GetCertificateFromFullPath(string path)
        {
            path = Path.Combine(Environment.CurrentDirectory, path);
            Assert.IsTrue(resolver.TryGetCertificateFromPath(path, out X509Certificate2 cert));
            Assert.IsNotNull(cert);
        }

        [TestCase("https://cacerts.digicert.com/DigiCertGlobalRootG3.crt")]
        public void GetCertificateFromUrl(string url)
        {
            Uri uri = new Uri(url);
            Assert.IsTrue(resolver.TryGetCertificateFromUrl(uri, out X509Certificate2 cert));
            Assert.IsNotNull(cert);
        }

        [TestCase("https://cacerts.digicert.com/DigiCertGlobalRootG3.crt")]
        public void GetCertificateFromPathUrl(string url)
        {
            Assert.IsTrue(resolver.TryGetCertificateFromPath(url, out X509Certificate2 cert));
            Assert.IsNotNull(cert);
        }

        [Test]
        public void GetCertificateFromDirectory()
        {
            Assert.IsTrue(resolver.TryGetCertificateFromDirectory(out X509Certificate2 cert));
            Assert.IsNotNull(cert);
        }
    }
}
