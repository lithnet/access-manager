using Lithnet.AccessManager.Agent.Providers;
using Lithnet.AccessManager.Api.Shared;
using System;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Agent.Authentication
{
    public class IwaTokenProvider : ITokenProvider
    {
        private readonly IAmsApiHttpClient httpClient;
        private readonly IAgentSettings settings;
        private TokenResponse token;

        public IwaTokenProvider(IAmsApiHttpClient httpClient, IAgentSettings settings)
        {
            this.httpClient = httpClient;
            this.settings = settings;
        }

        public async Task<string> GetAccessToken()
        {
            if (!this.settings.AmsServerManagementEnabled || this.settings.AuthenticationMode != AgentAuthenticationMode.Iwa)
            {
                throw new InvalidOperationException("IWA authentication is not enabled");
            }

            if (this.token != null)
            {
                if (!this.token.ExpiryDate.HasValue || this.token.ExpiryDate.Value.AddMinutes(-1) < DateTime.UtcNow)
                {
                    return (await this.RequestAccessToken()).Token;
                }

                return this.token.Token;
            }

            return (await this.RequestAccessToken()).Token;
        }

        private async Task<TokenResponse> RequestAccessToken()
        {
            this.token = await this.httpClient.RequestAccessTokenIwaAsync();
            return this.token;
        }
    }
}
