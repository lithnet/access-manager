using Lithnet.AccessManager.Agent.Providers;
using Lithnet.AccessManager.Api.Shared;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Agent.Authentication
{
    public class TokenProvider : ITokenProvider
    {
        private readonly ISettingsProvider settings;
        private readonly ITokenProvider selectedTokenProvider;

        public TokenProvider(ISettingsProvider settings, X509TokenProvider x509Provider)
        {
            this.settings = settings;
            this.selectedTokenProvider = x509Provider;
        }

        public TokenProvider(ISettingsProvider settings, IwaTokenProvider iwaProvider, X509TokenProvider x509Provider)
        {
            this.settings = settings;

            if (this.settings.AuthenticationMode == AuthenticationMode.Iwa)
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
