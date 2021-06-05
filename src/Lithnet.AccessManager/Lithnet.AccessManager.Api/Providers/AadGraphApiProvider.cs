using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using Microsoft.Graph.Auth;
using Microsoft.Identity.Client;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace Lithnet.AccessManager.Api.Providers
{
    public class AadGraphApiProvider : IAadGraphApiProvider
    {
        private IGraphServiceClient client;
        private readonly IOptions<AzureAdOptions> azureAdOptions;

        public AadGraphApiProvider(IOptions<AzureAdOptions> azureAdOptions)
        {
            this.azureAdOptions = azureAdOptions;
            this.Initialize();
        }

        private void Initialize()
        {
            IConfidentialClientApplication confidentialClientApplication = ConfidentialClientApplicationBuilder
                .Create(this.azureAdOptions.Value.ClientId)
                .WithTenantId(this.azureAdOptions.Value.TenantId)
                .WithClientSecret(this.azureAdOptions.Value.ClientSecret)
                .Build();

            ClientCredentialProvider authProvider = new ClientCredentialProvider(confidentialClientApplication);
            this.client = new GraphServiceClient(authProvider);
        }

        public async Task<Microsoft.Graph.Device> GetAadDeviceAsync(string deviceId)
        {
            IGraphServiceDevicesCollectionPage aadDevices = await this.client.Devices.Request().Filter($"deviceId eq '{WebUtility.UrlEncode(deviceId)}'").GetAsync();
            
            if (aadDevices.Count == 0)
            {
                throw new AadDeviceNotFoundException($"Object with device ID {deviceId} was not found in the directory");
            }

            if (aadDevices.Count > 1)
            {
                throw new AmbiguousNameException($"Multiple objects were found in the directory with the device ID {deviceId}");
            }

            return aadDevices.CurrentPage[0];
        }
    }
}
