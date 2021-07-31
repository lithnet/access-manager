using Lithnet.AccessManager.Agent.Providers;
using Lithnet.AccessManager.Api.Shared;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Agent.Authentication
{
    public class TokenProvider : ITokenProvider
    {
        private readonly ITokenProvider selectedTokenProvider;

        public TokenProvider(X509TokenProvider x509Provider)
        {
            this.selectedTokenProvider = x509Provider;
        }

        public TokenProvider(IAgentSettings settings, X509TokenProvider x509Provider, IwaTokenProvider iwaProvider) : this(x509Provider)
        {
            if (settings.AuthenticationMode == AgentAuthenticationMode.Iwa)
            {
                this.selectedTokenProvider = iwaProvider;
            }
            else
            {
                this.selectedTokenProvider = x509Provider;
            }
        }

        public async Task<string> GetAccessToken()
        {
            return await this.selectedTokenProvider.GetAccessToken();
        }
    }
}
