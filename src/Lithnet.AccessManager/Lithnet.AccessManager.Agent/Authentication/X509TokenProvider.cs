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
        private readonly ISettingsProvider settings;
        private readonly IAuthenticationCertificateProvider certProvider;
        private readonly ITokenClaimProvider claimProvider;

        private TokenResponse token;

        public X509TokenProvider(IHttpClientFactory httpClientFactory, ISettingsProvider settings, IAuthenticationCertificateProvider certProvider, ITokenClaimProvider claimProvider)
        {
            this.httpClientFactory = httpClientFactory;
            this.settings = settings;
            this.certProvider = certProvider;
            this.claimProvider = claimProvider;
        }

        public async Task<string> GetAccessToken()
        {
            if (!this.settings.AdvancedAgentEnabled || 
                (this.settings.AuthenticationMode != AgentAuthenticationMode.Ssa && 
                 this.settings.AuthenticationMode != AgentAuthenticationMode.Aadj))
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
            return await this.RequestAccessToken(await this.certProvider.GetCertificate());
        }

        private async Task<TokenResponse> RequestAccessToken(X509Certificate2 certificate)
        {
            string url = $"https://{this.settings.Server}/api/v1.0/auth/x509";
            ClientAssertion assertion = new ClientAssertion { Assertion = await this.BuildAssertion(certificate, url) };

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
