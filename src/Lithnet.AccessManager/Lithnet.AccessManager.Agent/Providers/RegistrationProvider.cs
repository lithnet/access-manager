using Lithnet.AccessManager.Agent.Providers;
using Lithnet.AccessManager.Api.Shared;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Lithnet.AccessManager.Agent
{
    public class RegistrationProvider : IRegistrationProvider
    {
        private readonly IHttpClientFactory httpClientFactory;
        private readonly JsonSerializerOptions jsonOptions;
        private readonly IAgentSettings settingsProvider;
        private readonly IAuthenticationCertificateProvider authCertProvider;
        private readonly ILogger<RegistrationProvider> logger;
        private readonly IAgentCheckInProvider agentCheckinProvider;

        public RegistrationProvider(IHttpClientFactory httpClientFactory, JsonSerializerOptions jsonOptions, IAgentSettings settingsProvider, IAuthenticationCertificateProvider authCertProvider, ILogger<RegistrationProvider> logger, IAgentCheckInProvider agentCheckinProvider)
        {
            this.httpClientFactory = httpClientFactory;
            this.jsonOptions = jsonOptions;
            this.settingsProvider = settingsProvider;
            this.authCertProvider = authCertProvider;
            this.logger = logger;
            this.agentCheckinProvider = agentCheckinProvider;
        }

        public async Task<RegistrationState> GetRegistrationState()
        {
            if (this.settingsProvider.RegistrationState != RegistrationState.Pending)
            {
                return this.settingsProvider.RegistrationState;
            }

            this.logger.LogTrace("The agent registration is pending approval. Calling the API to see if it has been approved yet");

            if (string.IsNullOrWhiteSpace(this.settingsProvider.CheckRegistrationUrl))
            {
                throw new InvalidOperationException("The client is pending registration approval, but the registration check URL is not present");
            }

            using var client = this.httpClientFactory.CreateClient(Constants.HttpClientAuthAnonymous);
            using var httpResponseMessage = await client.GetAsync(this.settingsProvider.CheckRegistrationUrl);
            return await this.GetRegistrationStateFromHttpResponse(httpResponseMessage);
        }

        private async Task<RegistrationState> GetRegistrationStateFromHttpResponse(HttpResponseMessage httpResponseMessage)
        {
            var responseString = await httpResponseMessage.Content.ReadAsStringAsync();

            if (httpResponseMessage.IsSuccessStatusCode || httpResponseMessage.StatusCode == HttpStatusCode.Forbidden)
            {
                var response = JsonSerializer.Deserialize<RegistrationResponse>(responseString, this.jsonOptions) ?? throw new UnexpectedResponseException("There response body returned by the server was invalid");

                this.settingsProvider.ClientId = response.ClientId;

                if (httpResponseMessage.StatusCode == HttpStatusCode.OK)
                {
                    this.logger.LogInformation("The agent registration has been approved");
                    this.settingsProvider.RegistrationState = RegistrationState.Approved;
                    this.settingsProvider.CheckRegistrationUrl = null;
                }
                else if (httpResponseMessage.StatusCode == HttpStatusCode.Accepted)
                {
                    this.logger.LogTrace("The agent registration is pending approval");

                    if (!httpResponseMessage.Headers.TryGetValues("Location", out IEnumerable<string> values))
                    {
                        throw new UnexpectedResponseException($"The API did not return the location header");
                    }

                    this.settingsProvider.CheckRegistrationUrl = values.First();
                    this.settingsProvider.RegistrationState = RegistrationState.Pending;
                }
                else if (httpResponseMessage.StatusCode == HttpStatusCode.Forbidden)
                {
                    this.logger.LogError("The agent registration request was rejected");
                    this.settingsProvider.RegistrationState = RegistrationState.Rejected;
                    this.settingsProvider.CheckRegistrationUrl = null;
                }
                else
                {
                    throw new UnexpectedResponseException($"The API returned an unexpected status code of {httpResponseMessage.StatusCode}");
                }

                this.settingsProvider.RegistrationKey = null;

                return this.settingsProvider.RegistrationState;
            }

            if (httpResponseMessage.StatusCode == HttpStatusCode.NotFound)
            {
                this.settingsProvider.RegistrationState = RegistrationState.NotRegistered;
                this.settingsProvider.ClientId = null;
                this.settingsProvider.CheckRegistrationUrl = null;

                this.logger.LogError("The agent was pending registration, but the server no longer has a record of the registration request. The agent will reset the registration data, and try again on the next run");
                return RegistrationState.NotRegistered;
            }

            throw httpResponseMessage.CreateException(responseString);
        }

        public bool CanRegisterAgent()
        {
            return !string.IsNullOrWhiteSpace(this.settingsProvider.RegistrationKey) &&
                   this.settingsProvider.RegistrationState != RegistrationState.Approved &&
                   this.settingsProvider.RegistrationState != RegistrationState.Pending;
        }

        public async Task<RegistrationState> RegisterAgent()
        {
            this.logger.LogInformation("Attempting to register the agent");
            using var client = this.httpClientFactory.CreateClient(Constants.HttpClientAuthAnonymous);

            var cert = await this.authCertProvider.GetOrCreateAgentCertificate();

            List<Claim> additionalClaims = new List<Claim>
            {
                new Claim("data", JsonSerializer.Serialize(await this.agentCheckinProvider.GenerateCheckInData())),
                new Claim("registration-key", this.settingsProvider.RegistrationKey),
            };

            var assertion = this.GenerateAssertion(cert, new Uri(client.BaseAddress, "agent/register").ToString(), additionalClaims.ToArray());

            using var httpResponseMessage = await client.PostAsync($"agent/register", assertion.AsJsonStringContent());
            return await this.GetRegistrationStateFromHttpResponse(httpResponseMessage);
        }

        public async Task RegisterSecondaryCredentials()
        {
            if (this.settingsProvider.AuthenticationMode != AgentAuthenticationMode.Aad)
            {
                throw new NotSupportedException("Secondary credentials can only be registered for AAD managed devices");
            }

            this.logger.LogInformation("Attempting to register the agent secondary credentials");
            using var client = this.httpClientFactory.CreateClient(Constants.HttpClientAuthBearer);

            var cert = await this.authCertProvider.GetOrCreateAgentCertificate();
            var assertion = this.GenerateAssertion(cert, new Uri(client.BaseAddress, "agent/register/credential").ToString());

            using var httpResponseMessage = await client.PostAsync($"agent/register/credential", assertion.AsJsonStringContent());
            httpResponseMessage.EnsureSuccessStatusCode();

            this.logger.LogInformation($"Successfully registered certificate thumbprint {cert.Thumbprint} with the AMS server");
            this.settingsProvider.HasRegisteredSecondaryCredentials = true;
        }

        private ClientAssertion GenerateAssertion(X509Certificate2 cert, string audience, params Claim[] additionalClaims)
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
                }),
                Expires = DateTime.UtcNow.AddMinutes(4),
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = new SigningCredentials(rsaSecurityKey, SecurityAlgorithms.RsaSsaPssSha384)
            };

            if (additionalClaims != null)
            {
                tokenDescriptor.Subject.AddClaims(additionalClaims);
            }

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