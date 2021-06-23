using Lithnet.AccessManager.Agent.Providers;
using Lithnet.AccessManager.Api.Shared;
using System;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Agent
{
    public class MetadataProvider : IMetadataProvider
    {
        private readonly IHttpClientFactory httpClientFactory;
        private readonly ISettingsProvider settingsProvider;
        private X509Certificate2 cachedCertificate;
        private DateTime lastMetadataRetrieval;
        private MetadataResponse cachedMetadata;

        public MetadataProvider(IHttpClientFactory httpClientFactory,  ISettingsProvider settingsProvider)
        {
            this.httpClientFactory = httpClientFactory;
            this.settingsProvider = settingsProvider;
        }


        public async Task<MetadataResponse> GetMetadata()
        {
            if (this.cachedMetadata == null || lastMetadataRetrieval.Add(this.settingsProvider.MetadataCacheDuration) < DateTime.UtcNow)
            {
                this.cachedMetadata = await RetrieveMetadata();
                this.lastMetadataRetrieval = DateTime.UtcNow;
            }

            return this.cachedMetadata;
        }


        private async Task<MetadataResponse> RetrieveMetadata()
        {
            using var client = this.httpClientFactory.CreateClient(Constants.HttpClientAuthAnonymous);
            using var httpResponseMessage = await client.GetAsync($"agent/metadata");

            var responseString = await httpResponseMessage.Content.ReadAsStringAsync();
            httpResponseMessage.EnsureSuccessStatusCode(responseString);

            return JsonSerializer.Deserialize<MetadataResponse>(responseString);
        }

        private async Task RefreshCertificateFromMetadata()
        {
            var metadata = await this.GetMetadata();
            this.cachedCertificate = new X509Certificate2(Convert.FromBase64String(metadata.PasswordManagement.EncryptionCertificate));
        }

        public async Task<X509Certificate2> GetEncryptionCertificate(string thumbprint)
        {
            if (this.cachedCertificate == null || (!(string.IsNullOrWhiteSpace(thumbprint) && !string.Equals(this.cachedCertificate.Thumbprint, thumbprint))))
            {
                await this.RefreshCertificateFromMetadata();
            }

            if (string.IsNullOrWhiteSpace(thumbprint))
            {
                return this.cachedCertificate;
            }

            if (string.Equals(this.cachedCertificate.Thumbprint, thumbprint))
            {
                return this.cachedCertificate;
            }

            throw new CertificateNotFoundException($"Could not find a certificate with the thumbprint {thumbprint}");
        }
    }
}
