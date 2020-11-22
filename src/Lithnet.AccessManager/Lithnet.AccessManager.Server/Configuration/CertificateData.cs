using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Lithnet.AccessManager.Server.Configuration
{
    public class CertificateData
    {
        public string Thumbprint { get; set; }

        public string Data { get; set; }

        public ProtectedSecret Secret { get; set; }

        public CertificateOperation Operation { get; set; }
    }
}
