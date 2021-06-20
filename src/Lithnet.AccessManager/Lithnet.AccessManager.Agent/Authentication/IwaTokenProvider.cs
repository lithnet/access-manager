using Lithnet.AccessManager.Agent.Providers;
using Lithnet.AccessManager.Api.Shared;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Agent.Authentication
{
    public class IwaTokenProvider : ITokenProvider
    {
        private readonly IHttpClientFactory httpClientFactory;
        private readonly ISettingsProvider settings;
        private TokenResponse token;

        public IwaTokenProvider(IHttpClientFactory httpClientFactory, ISettingsProvider settings)
        {
            this.httpClientFactory = httpClientFactory;
            this.settings = settings;
        }

        public async Task<string> GetAccessToken()
        {
            if (!this.settings.AdvancedAgentEnabled || this.settings.AuthenticationMode != AgentAuthenticationMode.Iwa)
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
            using var client = this.httpClientFactory.CreateClient(Constants.HttpClientAuthIwa);
            using var httpResponseMessage = await client.GetAsync("auth/iwa");


            var responseString = await httpResponseMessage.Content.ReadAsStringAsync();
            httpResponseMessage.EnsureSuccessStatusCode(responseString);

            this.token  = JsonSerializer.Deserialize<TokenResponse>(responseString);

            return this.token;
        }
    }
}
