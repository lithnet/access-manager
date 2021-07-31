using Lithnet.AccessManager.Api.Shared;
using Lithnet.AccessManager.Cryptography;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Agent.Providers
{
    public class AmsApiPasswordStorageProvider : IPasswordStorageProvider
    {
        private readonly IAmsApiHttpClient httpClient;
        private readonly IEncryptionProvider encryptionProvider;
        private readonly ILogger<AmsApiPasswordStorageProvider> logger;

        private IPasswordPolicy policy;
        private string passwordId;
        private X509Certificate2 encryptionCertificate;

        public AmsApiPasswordStorageProvider(IEncryptionProvider encryptionProvider, ILogger<AmsApiPasswordStorageProvider> logger, IAmsApiHttpClient httpClient)
        {
            this.encryptionProvider = encryptionProvider;
            this.logger = logger;
            this.httpClient = httpClient;
        }

        public async Task<bool> IsPasswordChangeRequired()
        {
            this.ResetState();

            this.logger.LogTrace("Checking to see if a password change is required");

            var response = await this.httpClient.GetPasswordChangeRequiredAsync();

            if (response == null)
            {
                this.logger.LogTrace("A password change is not currently required");
                return false;
            }

            this.logger.LogTrace("The server indicated that a password change is required");
            this.ValidatePasswordGetResponse(response);
            this.encryptionCertificate = new X509Certificate2(Convert.FromBase64String(response.EncryptionCertificate));
            this.policy = response.Policy;

            return true;
        }

        private void ValidatePasswordGetResponse(PasswordGetResponse response)
        {
            if (response == null)
            {
                throw new UnexpectedResponseException("The server returned an unexpected response");
            }

            if (string.IsNullOrWhiteSpace(response.EncryptionCertificate))
            {
                throw new UnexpectedResponseException("The API requested a password change, but did not supply an encryption certificate to use");
            }

            if (response.Policy == null)
            {
                throw new UnexpectedResponseException("The API did not return a password policy");
            }
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

            var response = await this.httpClient.UpdatePasswordAsync(request);
            this.logger.LogTrace("The password details were successfully submitted to the AMS API");

            if (response == null)
            {
                throw new UnexpectedResponseException("The server returned an unexpected response");
            }

            this.passwordId = response.PasswordId;
        }

        public async Task RollbackPasswordUpdate()
        {
            this.logger.LogTrace("Attempting to rollback the most recent password details sent to the AMS API");

            if (this.passwordId == null)
            {
                throw new InvalidOperationException("The rollback operation could not be completed because there was recent password change operation");
            }

            await this.httpClient.RollbackPasswordUpdateAsync(this.passwordId);
            this.ResetState();
            this.logger.LogTrace("The rollback was completed successfully");
        }

        public Task Commit()
        {
            this.ResetState();
            return Task.CompletedTask;
        }

        public IPasswordPolicy GetPolicy()
        {
            return this.policy ?? throw new InvalidOperationException("The was no password policy present");
        }

        private void ResetState()
        {
            this.passwordId = null;
            this.encryptionCertificate = null;
            this.policy = null;
        }
    }
}
