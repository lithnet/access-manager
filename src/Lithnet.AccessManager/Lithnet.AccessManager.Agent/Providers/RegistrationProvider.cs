using Lithnet.AccessManager.Api.Shared;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Agent.Providers
{
    public class RegistrationProvider : IRegistrationProvider
    {
        private readonly IAmsApiHttpClient httpClient;
        private readonly IAgentSettings settingsProvider;
        private readonly IAuthenticationCertificateProvider authCertProvider;
        private readonly ILogger<RegistrationProvider> logger;
        private readonly IAgentCheckInProvider agentCheckinProvider;
        private readonly IClientAssertionProvider clientAssertionProvider;

        public RegistrationProvider(IAmsApiHttpClient httpClient, IAgentSettings settingsProvider, IAuthenticationCertificateProvider authCertProvider, ILogger<RegistrationProvider> logger, IAgentCheckInProvider agentCheckinProvider, IClientAssertionProvider clientAssertionProvider)
        {
            this.httpClient = httpClient;
            this.settingsProvider = settingsProvider;
            this.authCertProvider = authCertProvider;
            this.logger = logger;
            this.agentCheckinProvider = agentCheckinProvider;
            this.clientAssertionProvider = clientAssertionProvider;
        }

        public async Task<RegistrationState> RegisterAgent()
        {
            this.logger.LogTrace("Attempting to register the agent");

            return await this.GetRegistrationResponse();
        }

        public async Task<RegistrationState> GetRegistrationState()
        {
            if (this.settingsProvider.RegistrationState != RegistrationState.Pending)
            {
                return this.settingsProvider.RegistrationState;
            }

            this.logger.LogTrace("Attempting to get the registration status for this agent");

            return await this.GetRegistrationResponse();
        }

        private async Task<RegistrationState> GetRegistrationResponse()
        {
            var cert = await this.authCertProvider.GetOrCreateAgentCertificate();

            List<Claim> additionalClaims = await this.BuildAdditionalClaims();
            var assertion = await this.clientAssertionProvider.BuildAssertion(cert, this.httpClient.BuildUrl("agent/register"), additionalClaims);

            try
            {
                var registrationResponse = await this.httpClient.RegisterAgentAsync(assertion);
                return this.GetRegistrationStateFromResponse(registrationResponse);
            }
            catch (ApiException ex)
            {
                if (ex.ApiErrorCode == ApiConstants.InvalidRegistrationKey)
                {
                    this.logger.LogError(EventIDs.AmsRegistrationInvalidRegistrationKey, "The agent registration request failed because the registration key provided was not accepted");
                    this.settingsProvider.RegistrationState = RegistrationState.Rejected;
                    this.settingsProvider.RegistrationKey = null;
                    return this.settingsProvider.RegistrationState;
                }

                throw;
            }
        }

        private async Task<List<Claim>> BuildAdditionalClaims()
        {
            List<Claim> additionalClaims = new List<Claim>
            {
                new Claim(AmsClaimNames.Data, JsonSerializer.Serialize(await this.agentCheckinProvider.GenerateCheckInData())),
            };

            if (!string.IsNullOrWhiteSpace(this.settingsProvider.RegistrationKey))
            {
                additionalClaims.Add(new Claim(AmsClaimNames.RegistrationKey, this.settingsProvider.RegistrationKey));
            }

            return additionalClaims;
        }

        private RegistrationState GetRegistrationStateFromResponse(RegistrationResponse response)
        {
            this.settingsProvider.ClientId = response.ClientId;

            if (response.ApprovalState == ApprovalState.Approved)
            {
                this.logger.LogInformation(EventIDs.AmsRegistrationApproved, "The agent registration has been approved");
                this.settingsProvider.RegistrationState = RegistrationState.Approved;
                this.settingsProvider.RegistrationKey = null;
                return this.settingsProvider.RegistrationState;
            }
            else if (response.ApprovalState == ApprovalState.Rejected)
            {
                this.logger.LogError(EventIDs.AmsRegistrationRejected, "The agent registration request was rejected");
                this.settingsProvider.RegistrationState = RegistrationState.Rejected;
                this.settingsProvider.RegistrationKey = null;
                return this.settingsProvider.RegistrationState;
            }
            else if (response.ApprovalState == ApprovalState.Pending)
            {
                this.logger.LogTrace("The agent registration is pending approval");

                this.settingsProvider.RegistrationState = RegistrationState.Pending;
                this.settingsProvider.RegistrationKey = null;
                return this.settingsProvider.RegistrationState;
            }
            else
            {
                throw new UnexpectedResponseException($"The server returned an unknown approval state value: {response.ApprovalState}");
            }
        }

        public bool CanRegisterAgent()
        {
            return !string.IsNullOrWhiteSpace(this.settingsProvider.RegistrationKey) &&
                   this.settingsProvider.RegistrationState != RegistrationState.Approved &&
                   this.settingsProvider.RegistrationState != RegistrationState.Pending;
        }

        public async Task RegisterSecondaryCredentials()
        {
            if (this.settingsProvider.AuthenticationMode != AgentAuthenticationMode.Aad)
            {
                throw new NotSupportedException("Secondary credentials can only be registered for AAD managed devices");
            }

            this.logger.LogTrace("Attempting to register the agent secondary credentials");

            var cert = await this.authCertProvider.GetOrCreateAgentCertificate();
            var assertion = await this.clientAssertionProvider.BuildAssertion(cert, this.httpClient.BuildUrl("agent/register/credential"));

            await this.httpClient.RegisterSecondaryCredentialsAsync(assertion);

            this.logger.LogInformation(EventIDs.RegisteredSecondaryCredentials, $"Successfully registered certificate thumbprint {cert.Thumbprint} with the AMS server");
            this.settingsProvider.HasRegisteredSecondaryCredentials = true;
        }
    }
}