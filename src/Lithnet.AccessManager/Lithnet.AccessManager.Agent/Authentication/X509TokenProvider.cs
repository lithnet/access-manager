using Lithnet.AccessManager.Agent.Providers;
using Lithnet.AccessManager.Api.Shared;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Agent.Authentication
{
    public class X509TokenProvider : ITokenProvider
    {
        private readonly IHttpClientFactory httpClientFactory;
        private readonly IAgentSettings settings;
        private readonly IAuthenticationCertificateProvider certProvider;
        private readonly ITokenClaimProvider claimProvider;

        private TokenResponse token;

        public X509TokenProvider(IHttpClientFactory httpClientFactory, IAgentSettings settings, IAuthenticationCertificateProvider certProvider, ITokenClaimProvider claimProvider)
        {
            this.httpClientFactory = httpClientFactory;
            this.settings = settings;
            this.certProvider = certProvider;
            this.claimProvider = claimProvider;
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
            return await this.RequestAccessToken(assertion);
        }

        private async Task<ClientAssertion> BuildAssertion(X509Certificate2 certificate)
        {
            string url = $"https://{this.settings.Server.Trim()}/api/v1.0/auth/x509";
            return new ClientAssertion { Assertion = await this.BuildAssertion(certificate, url) };
        }

        private async Task<TokenResponse> RequestAccessToken(ClientAssertion assertion)
        {
            using var client = this.httpClientFactory.CreateClient(Constants.HttpClientAuthAnonymous);
            using var httpResponseMessage = await client.PostAsync("auth/x509", assertion.AsJsonStringContent());

            var responseString = await httpResponseMessage.Content.ReadAsStringAsync();
            httpResponseMessage.EnsureSuccessStatusCode(responseString);

            this.token = JsonSerializer.Deserialize<TokenResponse>(responseString);

            return this.token;
        }

        private async Task<string> BuildAssertion(X509Certificate2 cert, string audience)
        {
            string hostname = Environment.MachineName;

            RsaSecurityKey rsaSecurityKey = new RsaSecurityKey(cert.GetRSAPrivateKey());

            string exportedCertificate = Convert.ToBase64String(cert.Export(X509ContentType.Cert));

            string myIssuer = hostname;

            JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
            SecurityTokenDescriptor tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("jti", Guid.NewGuid().ToString()),
                }),
                IssuedAt = DateTime.UtcNow,
                Expires = DateTime.UtcNow.AddMinutes(4),
                Issuer = myIssuer,
                Audience = audience,
                SigningCredentials = new SigningCredentials(rsaSecurityKey, SecurityAlgorithms.RsaSha256)
            };

            await this.claimProvider.AddClaims(tokenDescriptor);

            // Add x5c header parameter containing the signing certificate:
            JwtSecurityToken jwt = (JwtSecurityToken) tokenHandler.CreateToken(tokenDescriptor);
            jwt.Header.Add(JwtHeaderParameterNames.X5c, new List<string> {exportedCertificate});

            return tokenHandler.WriteToken(jwt);
        }
    }
}
