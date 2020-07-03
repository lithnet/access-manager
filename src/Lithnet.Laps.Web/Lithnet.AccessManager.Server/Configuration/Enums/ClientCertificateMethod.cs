using System;
using System.Collections.Generic;
using System.Text;

namespace Lithnet.AccessManager.Configuration
{
    public enum ClientCertificateMethod
    {
        NoCertificate = 0,
        AllowCertificate = 1,
        AllowRenegotation = 2
    }
}
