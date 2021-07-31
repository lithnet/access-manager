using Microsoft.Extensions.Logging;
using System;
using System.Security.Cryptography.X509Certificates;

namespace Lithnet.AccessManager.Agent.Providers
{
    public class LinuxAadJoinInformationProvider : IAadJoinInformationProvider
    {
        private readonly ILogger<LinuxAadJoinInformationProvider> logger;
        private readonly IAgentSettings agentSettings;

        public LinuxAadJoinInformationProvider(ILogger<LinuxAadJoinInformationProvider> logger, IAgentSettings agentSettings)
        {
            this.logger = logger;
            this.agentSettings = agentSettings;
        }

        public bool InitializeJoinInformation()
        {
            return false;
        }

        public bool IsWorkplaceJoined => false;

        public bool IsDeviceJoined => false;

        public string DeviceId => null;

        public string TenantId => null;

        public X509Certificate2 GetAadCertificate()
        {
            throw new NotImplementedException();
        }

        public T DelegateCertificateOperation<T>(Func<X509Certificate2, T> signingDelegate)
        {
            throw new NotImplementedException();
        }
    }
}