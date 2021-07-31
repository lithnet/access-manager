using Microsoft.Extensions.Logging;
using System.Security.Cryptography.X509Certificates;

namespace Lithnet.AccessManager.Agent.Providers
{
    public class LinuxAuthenticationCertificateProvider : AuthenticationCertificateProvider
    {
        public LinuxAuthenticationCertificateProvider(ILogger<AuthenticationCertificateProvider> logger, IAgentSettings settings)
            : base(logger, settings, StoreLocation.CurrentUser)
        {
        }
    }
}
