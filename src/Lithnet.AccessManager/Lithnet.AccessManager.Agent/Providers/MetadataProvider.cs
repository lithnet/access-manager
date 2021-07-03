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
        private readonly IAgentSettings settingsProvider;
        private DateTime lastMetadataRetrieval;
        private MetadataResponse cachedMetadata;

        public MetadataProvider(IHttpClientFactory httpClientFactory, IAgentSettings settingsProvider)
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
            using (var client = this.httpClientFactory.CreateClient(Constants.HttpClientAuthAnonymous))
            {
                using (var httpResponseMessage = await client.GetAsync($"agent/metadata"))
                {

                    var responseString = await httpResponseMessage.Content.ReadAsStringAsync();
                    httpResponseMessage.EnsureSuccessStatusCode(responseString);

                    return JsonSerializer.Deserialize<MetadataResponse>(responseString);
                }
            }
        }
    }
}