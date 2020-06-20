using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Lithnet.AccessManager
{
    public class CertificateResolver : ICertificateResolver
    {
        public X509Certificate2 GetEncryptionCertificate(string thumbprint)
        {
            if (thumbprint == null)
            {
                throw new ConfigurationException("The thumbprint of the signing certificate was not provided");
            }

            return this.ResolveCertificateFromLocalStore(thumbprint);
        }

        public X509Certificate2 GetDecryptionCertificate(string thumbprint)
        {
            if (thumbprint == null)
            {
                throw new ConfigurationException("The thumbprint of the signing certificate was not provided");
            }

            var cert = this.ResolveCertificateFromLocalStore(thumbprint);

            if (!cert.HasPrivateKey)
            {
                throw new CertificateValidationException($"The certificate required to decrypt the data was found, but the private key was not available. Thumbprint: {thumbprint}");
            }

            return cert;
        }

        private X509Certificate2 ResolveCertificateFromLocalStore(string thumbprint)
        {
            return GetCertificateFromStore(thumbprint, StoreLocation.CurrentUser) ??
                    GetCertificateFromStore(thumbprint, StoreLocation.LocalMachine) ??
                    throw new CertificateNotFoundException($"A certificate with the thumbprint {thumbprint} could not be found");
        }

        private X509Certificate2 GetCertificateFromStore(string thumbprint, StoreLocation storeLocation)
        {
            X509Store store = new X509Store(StoreName.My, storeLocation);
            store.Open(OpenFlags.ReadOnly);

            try
            {
                foreach (var item in store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, false))
                {
                    return item;
                }
            }
            finally
            {
                if (store.IsOpen)
                {
                    store.Close();
                }
            }

            return null;
        }
    }
}
