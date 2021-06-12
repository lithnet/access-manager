using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Lithnet.AccessManager.Agent
{
    public interface IAuthenticationCertificateProvider
    {
        X509Certificate2 GetCertificate();
    }
}
