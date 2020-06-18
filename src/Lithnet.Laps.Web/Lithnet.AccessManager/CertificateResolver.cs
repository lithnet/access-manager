using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Lithnet.AccessManager
{
    public class CertificateResolver : ICertificateResolver
    {
        public X509Certificate2 GetEncryptionCertificate()
        {
            return null;
        }

        public X509Certificate2 GetDecryptionCertificate()
        {
            return null;
        }
    }
}
