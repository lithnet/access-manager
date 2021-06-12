using Lithnet.AccessManager.Agent.Providers;
using Lithnet.AccessManager.Api.Shared;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Threading.Tasks;
using Lithnet.AccessManager.Agent.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Lithnet.AccessManager.Agent
{
    public class RegistrationProvider : IRegistrationProvider
    {
        private readonly IHttpClientFactory httpClientFactory;
        private readonly JsonSerializerOptions jsonOptions;
        private readonly IWritableOptions<AppState> appState;
        private readonly ICertificateProvider certificateProvider;

        public RegistrationProvider(IHttpClientFactory httpClientFactory, JsonSerializerOptions jsonOptions, IWritableOptions<AppState> appState, ICertificateProvider certificateProvider)
        {
            this.httpClientFactory = httpClientFactory;
            this.jsonOptions = jsonOptions;
            this.appState = appState;
            this.certificateProvider = certificateProvider;
        }

        public async Task<RegistrationState> GetRegistrationState()
        {
            if (this.appState.Value.RegistrationState != RegistrationState.Pending)
            {
                return this.appState.Value.RegistrationState;
            }

            if (string.IsNullOrWhiteSpace(this.appState.Value.CheckRegistrationUrl))
            {
                throw new InvalidOperationException("The client is pending registration approval, but the registration check URL is not present");
            }

            using var client = this.httpClientFactory.CreateClient(Constants.HttpClientAuthAnonymous);
            using var httpResponseMessage = await client.GetAsync(this.appState.Value.CheckRegistrationUrl);
            return await this.GetRegistrationStateFromHttpResponse(httpResponseMessage);
        }

        private async Task<RegistrationState> GetRegistrationStateFromHttpResponse(HttpResponseMessage httpResponseMessage)
        {
            var responseString = await httpResponseMessage.Content.ReadAsStringAsync();

            if (httpResponseMessage.IsSuccessStatusCode || httpResponseMessage.StatusCode == HttpStatusCode.Forbidden)
            {
                var response = JsonSerializer.Deserialize<RegistrationResponse>(responseString, this.jsonOptions);
                this.appState.Value.ClientId = response.ClientId;

                if (httpResponseMessage.StatusCode == HttpStatusCode.OK)
                {
                    this.appState.Value.RegistrationState = RegistrationState.Approved;
                }
                else if (httpResponseMessage.StatusCode == HttpStatusCode.Accepted)
                {
                    if (!httpResponseMessage.Headers.TryGetValues("Location", out IEnumerable<string> values))
                    {
                        throw new UnexpectedResponseException($"The API did not return the location header");
                    }

                    this.appState.Value.CheckRegistrationUrl = values.First();
                    this.appState.Value.RegistrationState = RegistrationState.Pending;
                }
                else if (httpResponseMessage.StatusCode == HttpStatusCode.Forbidden)
                {
                    this.appState.Value.RegistrationState = RegistrationState.Rejected;
                }
                else
                {
                    throw new UnexpectedResponseException($"The API returned an unexpected status code of {httpResponseMessage.StatusCode}");
                }

                this.appState.Value.RegistrationKey = null;

                return this.appState.Value.RegistrationState;
            }

            throw httpResponseMessage.CreateException(responseString);
        }

        public bool CanRegisterAgent()
        {
            var state = this.appState.Value;
            return !string.IsNullOrWhiteSpace(state.RegistrationKey) &&
                   state.RegistrationState != RegistrationState.Approved &&
                   state.RegistrationState != RegistrationState.Pending;
        }

        public async Task<RegistrationState> RegisterAgent()
        {
            using var client = this.httpClientFactory.CreateClient(Constants.HttpClientAuthAnonymous);

            var cert = this.CreateSelfSignedCertificate();
            var assertion = this.GenerateAssertion(cert, client.BaseAddress.ToString());

            using var httpResponseMessage = await client.PostAsync($"agent/register", assertion.AsJsonStringContent());
            return await this.GetRegistrationStateFromHttpResponse(httpResponseMessage);
        }

        private X509Certificate2 CreateSelfSignedCertificate()
        {
            X509Certificate2 cert = this.certificateProvider.CreateSelfSignedCert(Environment.MachineName, new Oid(Constants.AgentAuthenticationCertificateOid));
            return cert;
        }

        private ClientAssertion GenerateAssertion(X509Certificate2 cert, string audience)
        {
            RsaSecurityKey rsaSecurityKey = new RsaSecurityKey(cert.GetRSAPrivateKey());
            string exportedCertificate = Convert.ToBase64String(cert.Export(X509ContentType.Cert));

            string issuer = Environment.MachineName;

            JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
            SecurityTokenDescriptor tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("jti", Guid.NewGuid().ToString()),
                    new Claim("hostname", Environment.MachineName),
                    new Claim("dnsname", Dns.GetHostName()),
                    new Claim("registration-key", this.appState.Value.RegistrationKey),
                }),
                Expires = DateTime.UtcNow.AddMinutes(4),
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = new SigningCredentials(rsaSecurityKey, SecurityAlgorithms.RsaSsaPssSha384)
            };

            // Add x5c header parameter containing the signing certificate:
            JwtSecurityToken token = (JwtSecurityToken)tokenHandler.CreateToken(tokenDescriptor);
            token.Header.Add(JwtHeaderParameterNames.X5c, new List<string> { exportedCertificate });

            string t = tokenHandler.WriteToken(token);

            return new ClientAssertion
            {
                Assertion = t
            };
        }
    }
}
