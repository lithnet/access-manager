using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Agent
{
    public interface IAuthenticationCertificateProvider
    {
        Task<X509Certificate2> GetCertificate();

        Task<X509Certificate2> GetOrCreateAgentCertificate();
    }
}
