using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Agent.Providers
{
    public interface IAadJoinInformationProvider
    {
        Task<X509Certificate2> GetAadCertificate();

        Task<string> GetDeviceId();

        Task<string> GetTenantId();
    }
}