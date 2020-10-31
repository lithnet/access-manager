using System;
using System.Collections.Generic;
using System.Text;

namespace Lithnet.AccessManager.Server.Configuration
{
    public class DataProtectionOptions
    {
        public int Usn { get; set; } = 0;

        public List<CertificateData> Certificates { get; set; } = new List<CertificateData>();

        public string AuthorizedSecretReaders { get; set; }

        public bool EnableCertificateSynchronization { get; set; }

        public bool EnableClusterCompatibleSecretEncryption { get; set; }
    }
}
