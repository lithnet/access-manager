using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Lithnet.AccessManager.Agent.Models;

namespace Lithnet.AccessManager.Agent.Providers
{
    public interface IAadJoinInformationProvider
    {
        Task<bool> InitializeJoinInformation();
        
        bool IsAadJoined { get; }
        
        bool IsWorkplaceJoined { get; }
        
        bool IsDeviceJoined { get; }
        
        string DeviceId { get; }
        
        string TenantId { get; }

        X509Certificate2 GetAadCertificate();

        T DelegateCertificateOperation<T>(Func<X509Certificate2, T> signingDelegate);
    }
}