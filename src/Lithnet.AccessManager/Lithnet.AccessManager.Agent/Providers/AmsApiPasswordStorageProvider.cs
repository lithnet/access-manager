using Lithnet.AccessManager.Agent.Providers;
using Lithnet.AccessManager.Api.Shared;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Lithnet.AccessManager.Agent
{
    public class AmsApiPasswordStorageProvider : IPasswordStorageProvider
    {
        private readonly IHttpClientFactory httpClientFactory;
        private readonly JsonSerializerOptions jsonOptions;
        private readonly IEncryptionProvider encryptionProvider;
        private readonly IMetadataProvider metadataProvider;
        private readonly ISettingsProvider settingsProvider;
        private readonly ILogger<AmsApiPasswordStorageProvider> logger;

        private string passwordId;
        private string lastEncryptionThumbprint;

        public AmsApiPasswordStorageProvider(IHttpClientFactory httpClientFactory, JsonSerializerOptions jsonOptions, IEncryptionProvider encryptionProvider, IMetadataProvider metadataProvider, ISettingsProvider settingsProvider, ILogger<AmsApiPasswordStorageProvider> logger)
        {
            this.httpClientFactory = httpClientFactory;
            this.jsonOptions = jsonOptions;
            this.encryptionProvider = encryptionProvider;
            this.metadataProvider = metadataProvider;
            this.settingsProvider = settingsProvider;
            this.logger = logger;
        }

        public async Task<bool> IsPasswordChangeRequired()
        {
            this.lastEncryptionThumbprint = null;
            this.logger.LogTrace("Checking to see if a password change is required");

            using var client = this.httpClientFactory.CreateClient(Constants.HttpClientAuthBearer);
            using var httpResponseMessage = await client.GetAsync($"agent/password");
            var responseString = await httpResponseMessage.Content.ReadAsStringAsync();

            if (httpResponseMessage.IsSuccessStatusCode)
            {
                if (httpResponseMessage.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    this.logger.LogTrace("A password change is not currently required");
                    return false;
                }

                if (httpResponseMessage.StatusCode == System.Net.HttpStatusCode.ResetContent)
                {
                    this.logger.LogInformation("A password change is required");

                    var response = JsonSerializer.Deserialize<PasswordGetResponse>(responseString, this.jsonOptions);
                    this.lastEncryptionThumbprint = response.EncryptionCertificateThumbprint;

                    if (string.IsNullOrWhiteSpace (this.lastEncryptionThumbprint))
                    {
                        throw new UnexpectedResponseException("The API requested a password change, but did not supply an encryption certificate thumbprint to use");
                    }
                    return true;
                }

                throw new UnexpectedResponseException($"The API returned an unexpected status code of {httpResponseMessage.StatusCode}");
            }

            httpResponseMessage.EnsureSuccessStatusCode(responseString);

            return false;
        }

        public async Task UpdatePassword(string accountName, string password, DateTime expiry)
        {
            this.passwordId = null;

            this.logger.LogTrace("Attempting to submit the new password details to the AMS API");

            PasswordUpdateRequest request = new PasswordUpdateRequest
            {
                AccountName = accountName,
                ExpiryDate = expiry,
                PasswordData = this.encryptionProvider.Encrypt(await this.metadataProvider.GetEncryptionCertificate(this.lastEncryptionThumbprint), password)
            };

            using var client = this.httpClientFactory.CreateClient(Constants.HttpClientAuthBearer);
            using var httpResponseMessage = await client.PostAsync($"agent/password", request.AsJsonStringContent());

            var responseString = await httpResponseMessage.Content.ReadAsStringAsync();
            httpResponseMessage.EnsureSuccessStatusCode(responseString);

            var response = JsonSerializer.Deserialize<PasswordUpdateResponse>(responseString, this.jsonOptions);

            this.logger.LogTrace("The password details were successfully submitted to the AMS API");

            this.passwordId = response.PasswordId;
        }

        public async Task RollbackPasswordUpdate()
        {
            this.logger.LogTrace("Attempting to rollback the most recent password details sent to the AMS API");

            if (this.passwordId == null)
            {
                throw new InvalidOperationException("The rollback operation could not be completed because there was recent password change operation");
            }

            using var client = this.httpClientFactory.CreateClient(Constants.HttpClientAuthBearer);
            using var httpResponseMessage = await client.DeleteAsync($"agent/password/{this.passwordId}");

            var responseString = await httpResponseMessage.Content.ReadAsStringAsync();
            httpResponseMessage.EnsureSuccessStatusCode(responseString);

            this.logger.LogTrace("The rollback was completed successfully");

        }

        public Task Commit()
        {
            this.passwordId = null;
            this.lastEncryptionThumbprint = null;
            return Task.CompletedTask;
        }
    }
}
