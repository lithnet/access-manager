using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using Microsoft.Graph.Auth;
using Microsoft.Identity.Client;
using System.Net;
using System.Security.Principal;
using System.Threading.Tasks;
using Lithnet.AccessManager.Api;
using Lithnet.AccessManager.Server;
using Microsoft.Extensions.Options;

namespace Lithnet.AccessManager.Server
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

        public async Task<IList<Group>> GetDeviceGroups(string objectId)
        {
            return await this.GetDeviceGroups(objectId, "displayName,Id");
        }

        public async Task<IList<Group>> GetDeviceGroups(string objectId, string selection)
        {
            var page = await this.client.Devices[objectId].TransitiveMemberOf
                .Request()
                .Select(selection)
                .GetAsync();

            List<Group> members = new List<Group>();

            if (page?.Count > 0)
            {
                members.AddRange(page.CurrentPage.OfType<Group>());

                while (page.NextPageRequest != null)
                {
                    page = await page.NextPageRequest.GetAsync();
                    members.AddRange(page.CurrentPage.OfType<Group>());
                }
            }

            return members;
        }

        public async Task<List<SecurityIdentifier>> GetDeviceGroupSids(string objectId)
        {
            List<SecurityIdentifier> sids = new List<SecurityIdentifier>();
            foreach (var group in await this.GetDeviceGroups(objectId, "securityIdentifier"))
            {
                if (!string.IsNullOrWhiteSpace(group.SecurityIdentifier))
                {
                    sids.Add(new SecurityIdentifier(group.SecurityIdentifier));
                }
            }

            return sids;
        }


        public async Task<Microsoft.Graph.Device> GetAadDeviceByIdAsync(string objectId)
        {
            try
            {
                return await this.client.Devices[objectId].Request().GetAsync();
            }
            catch (ServiceException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                throw new AadDeviceNotFoundException($"Object with object ID {objectId} was not found in the directory", ex);
            }
        }

        public async Task<Microsoft.Graph.Device> GetAadDeviceByDeviceIdAsync(string deviceId)
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
