using Lithnet.AccessManager.Api;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Auth;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Principal;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Server
{
    public class AadGraphApiProvider : IAadGraphApiProvider
    {
        private readonly IOptions<AzureAdOptions> azureAdOptions;
        private readonly Dictionary<string, IGraphServiceClient> clients;
        private readonly IProtectedSecretProvider protectedSecretProvider;

        public AadGraphApiProvider(IOptions<AzureAdOptions> azureAdOptions, IProtectedSecretProvider protectedSecretProvider)
        {
            this.azureAdOptions = azureAdOptions;
            this.protectedSecretProvider = protectedSecretProvider;
            this.clients = new Dictionary<string, IGraphServiceClient>(StringComparer.OrdinalIgnoreCase);
            this.Initialize();
        }

        private void Initialize()
        {
            foreach (var tenant in this.azureAdOptions.Value.Tenants)
            {
                IConfidentialClientApplication confidentialClientApplication = ConfidentialClientApplicationBuilder
                    .Create(tenant.ClientId)
                    .WithTenantId(tenant.TenantId)
                    .WithClientSecret(this.protectedSecretProvider.UnprotectSecret(tenant.ClientSecret))
                    .Build();

                ClientCredentialProvider authProvider = new ClientCredentialProvider(confidentialClientApplication);
                this.clients.Add(tenant.TenantId, new GraphServiceClient(authProvider));
            }
        }

        private IGraphServiceClient GetClient(string tenantId)
        {
            if (!this.clients.TryGetValue(tenantId, out IGraphServiceClient client))
            {
                throw new AadTenantNotFoundException($"The Azure AD tenant ID was not found in the configuration ({tenantId})");
            }

            return client;
        }

        public async Task<IList<Group>> GetDeviceGroups(string tenant, string objectId)
        {
            return await this.GetDeviceGroups(tenant, objectId, "displayName,Id");
        }

        public async Task<IList<Group>> GetDeviceGroups(string tenant, string objectId, string selection)
        {
            var page = await this.GetClient(tenant).Devices[objectId].TransitiveMemberOf
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

        public async Task<List<SecurityIdentifier>> GetDeviceGroupSids(string tenant, string objectId)
        {
            List<SecurityIdentifier> sids = new List<SecurityIdentifier>();
            foreach (var group in await this.GetDeviceGroups(tenant, objectId, "securityIdentifier"))
            {
                if (!string.IsNullOrWhiteSpace(group.SecurityIdentifier))
                {
                    sids.Add(new SecurityIdentifier(group.SecurityIdentifier));
                }
            }

            return sids;
        }


        public async Task<Microsoft.Graph.Device> GetAadDeviceByIdAsync(string tenant, string objectId)
        {
            try
            {
                return await this.GetClient(tenant).Devices[objectId].Request().GetAsync();
            }
            catch (ServiceException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                throw new AadDeviceNotFoundException($"Object with object ID {objectId} was not found in the directory", ex);
            }
        }

        public async Task<Microsoft.Graph.Device> GetAadDeviceByDeviceIdAsync(string tenant, string deviceId)
        {
            IGraphServiceDevicesCollectionPage aadDevices = await this.GetClient(tenant).Devices.Request().Filter($"deviceId eq '{WebUtility.UrlEncode(deviceId)}'").GetAsync();

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
