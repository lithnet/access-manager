using Lithnet.AccessManager.Agent.Providers;
using Lithnet.AccessManager.Api.Shared;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Agent.Authentication
{
    public class X509TokenProvider : ITokenProvider
    {
        private readonly IAmsApiHttpClient httpClient;
        private readonly IAgentSettings settings;
        private readonly IAuthenticationCertificateProvider certProvider;
        private readonly IEnumerable<ITokenClaimProvider> claimProviders;
        private readonly IClientAssertionProvider assertionProvider;

        private TokenResponse token;

        public X509TokenProvider(IAmsApiHttpClient httpClient, IAgentSettings settings, IAuthenticationCertificateProvider certProvider, IEnumerable<ITokenClaimProvider> claimProvider, IClientAssertionProvider assertionProvider)
        {
            this.httpClient = httpClient;
            this.settings = settings;
            this.certProvider = certProvider;
            this.claimProviders = claimProvider;
            this.assertionProvider = assertionProvider;
        }

        public async Task<string> GetAccessToken()
        {
            if (!this.settings.AmsServerManagementEnabled ||
                (this.settings.AuthenticationMode != AgentAuthenticationMode.Ams &&
                 this.settings.AuthenticationMode != AgentAuthenticationMode.Aad))
            {
                throw new InvalidOperationException("X509 authentication is not enabled");
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
            ClientAssertion assertion = await this.certProvider.DelegateCertificateOperation(this.BuildAssertion);
            this.token = await this.httpClient.RequestAccessTokenX509Async(assertion);
            return this.token;
        }

        private async Task<ClientAssertion> BuildAssertion(X509Certificate2 certificate)
        {
            List<Claim> additionalClaims = new List<Claim>();

            foreach (var claimsProvider in this.claimProviders)
            {
                additionalClaims.AddRange(claimsProvider.GetClaims());
            }

            return await this.assertionProvider.BuildAssertion(certificate, new Uri(this.httpClient.BaseAddress, "auth/x509").ToString(), additionalClaims);
        }
    }
}
