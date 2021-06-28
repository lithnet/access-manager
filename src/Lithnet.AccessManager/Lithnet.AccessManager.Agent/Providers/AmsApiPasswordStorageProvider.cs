using Lithnet.AccessManager.Api.Shared;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Agent
{
    public class AmsApiPasswordStorageProvider : IPasswordStorageProvider
    {
        private readonly IHttpClientFactory httpClientFactory;
        private readonly JsonSerializerOptions jsonOptions;
        private readonly IEncryptionProvider encryptionProvider;
        private readonly ILogger<AmsApiPasswordStorageProvider> logger;

        private string passwordId;
        private X509Certificate2 encryptionCertificate;

        public AmsApiPasswordStorageProvider(IHttpClientFactory httpClientFactory, JsonSerializerOptions jsonOptions, IEncryptionProvider encryptionProvider, ILogger<AmsApiPasswordStorageProvider> logger)
        {
            this.httpClientFactory = httpClientFactory;
            this.jsonOptions = jsonOptions;
            this.encryptionProvider = encryptionProvider;
            this.logger = logger;
        }

        public async Task<bool> IsPasswordChangeRequired()
        {
            this.ResetState();

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

                    if (response == null)
                    {
                        throw new UnexpectedResponseException("The server returned an unexpected response");
                    }

                    if (string.IsNullOrWhiteSpace(response.EncryptionCertificate))
                    {
                        throw new UnexpectedResponseException("The API requested a password change, but did not supply an encryption certificate to use");
                    }

                    this.encryptionCertificate = new X509Certificate2(Convert.FromBase64String(response.EncryptionCertificate));

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

            if (this.encryptionCertificate == null)
            {
                throw new NotSupportedException("There was no certificate present to perform the password encryption operation");
            }

            PasswordUpdateRequest request = new PasswordUpdateRequest
            {
                AccountName = accountName,
                ExpiryDate = expiry,
                PasswordData = this.encryptionProvider.Encrypt(this.encryptionCertificate, password)
            };

            using var client = this.httpClientFactory.CreateClient(Constants.HttpClientAuthBearer);
            using var httpResponseMessage = await client.PostAsync($"agent/password", request.AsJsonStringContent());

            var responseString = await httpResponseMessage.Content.ReadAsStringAsync();
            httpResponseMessage.EnsureSuccessStatusCode(responseString);

            var response = JsonSerializer.Deserialize<PasswordUpdateResponse>(responseString, this.jsonOptions);

            if (response == null)
            {
                throw new UnexpectedResponseException("The server returned an unexpected response");
            }

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

            this.ResetState();

            this.logger.LogTrace("The rollback was completed successfully");
        }

        public Task Commit()
        {
            this.ResetState();
            return Task.CompletedTask;
        }

        private void ResetState()
        {
            this.passwordId = null;
            this.encryptionCertificate = null;
        }
    }
}
