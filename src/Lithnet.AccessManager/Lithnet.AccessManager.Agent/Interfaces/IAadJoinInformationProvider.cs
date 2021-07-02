using System;
using System.Security.Cryptography.X509Certificates;

namespace Lithnet.AccessManager.Agent.Providers
{
    public interface IAadJoinInformationProvider
    {
        bool InitializeJoinInformation();
        
        bool IsWorkplaceJoined { get; }
        
        bool IsDeviceJoined { get; }
        
        string DeviceId { get; }
        
        string TenantId { get; }

        X509Certificate2 GetAadCertificate();

        T DelegateCertificateOperation<T>(Func<X509Certificate2, T> signingDelegate);
    }
}