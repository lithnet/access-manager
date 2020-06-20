using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Moq;
using NUnit.Framework;

namespace Lithnet.AccessManager.Test
{
    public class CertificateResolverTests
    {
        private Mock<NLog.ILogger> dummyLogger;

        [SetUp()]
        public void TestInitialize()
        {
            dummyLogger = new Mock<NLog.ILogger>();
        }

        [TestCase(StoreLocation.CurrentUser)]
        [TestCase(StoreLocation.LocalMachine)]
        public void GetEncryptionCertificateFromStore(StoreLocation location)
        {
            X509Store store = new X509Store(StoreName.My, location, OpenFlags.ReadOnly);
            CertificateResolver resolver = new CertificateResolver();

            foreach (var cert in store.Certificates)
            {
                Assert.AreEqual(cert, resolver.GetEncryptionCertificate(cert.Thumbprint));
            }

            Assert.Throws<CertificateNotFoundException>(() => resolver.GetEncryptionCertificate("ABCDE"));
        }

        [TestCase(StoreLocation.CurrentUser)]
        [TestCase(StoreLocation.LocalMachine)]
        public void GetDecryptionCertificateFromStore(StoreLocation location)
        {
            X509Store store = new X509Store(StoreName.My, location, OpenFlags.ReadOnly);
            CertificateResolver resolver = new CertificateResolver();
            
            foreach (var cert in store.Certificates)
            {
                if (cert.HasPrivateKey)
                {
                    Assert.AreEqual(cert, resolver.GetEncryptionCertificate(cert.Thumbprint));
                }
                else
                {
                    Assert.Throws<CertificateValidationException>(() => resolver.GetEncryptionCertificate(cert.Thumbprint));
                }
            }

            Assert.Throws<CertificateNotFoundException>(() => resolver.GetEncryptionCertificate("ABCDE"));
        }
    }
}
