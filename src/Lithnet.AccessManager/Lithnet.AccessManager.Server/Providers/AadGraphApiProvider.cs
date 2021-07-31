using Lithnet.AccessManager.Api;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Auth;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Security.Principal;
using System.Threading.Tasks;
using Lithnet.AccessManager.Server.Configuration;

namespace Lithnet.AccessManager.Server
{
    public class AadGraphApiProvider : IAadGraphApiProvider
    {
        private readonly IOptionsMonitor<AzureAdOptions> azureAdOptions;
        private readonly Dictionary<string, IGraphServiceClient> clients;
        private readonly IProtectedSecretProvider protectedSecretProvider;
        private readonly Dictionary<string, string> tenantNameCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public AadGraphApiProvider(IOptionsMonitor<AzureAdOptions> azureAdOptions, IProtectedSecretProvider protectedSecretProvider)
        {
            this.clients = new Dictionary<string, IGraphServiceClient>(StringComparer.OrdinalIgnoreCase);

            this.azureAdOptions = azureAdOptions;
            this.azureAdOptions.OnChange(x => this.Initialize());
            this.protectedSecretProvider = protectedSecretProvider;
            this.Initialize();
        }

        private void Initialize()
        {
            this.clients.Clear();

            foreach (var tenant in this.azureAdOptions.CurrentValue.Tenants)
            {
                IConfidentialClientApplication confidentialClientApplication = this.BuildConfidentialClientApplication(tenant.TenantId, tenant.ClientId, tenant.ClientSecret);
                ClientCredentialProvider authProvider = new ClientCredentialProvider(confidentialClientApplication);
                this.clients.Add(tenant.TenantId, new GraphServiceClient(authProvider));
            }
        }

        private IConfidentialClientApplication BuildConfidentialClientApplication(string tenantId, string clientId, ProtectedSecret clientSecret)
        {
            IConfidentialClientApplication confidentialClientApplication = ConfidentialClientApplicationBuilder
                .Create(clientId)
                .WithTenantId(tenantId)
                .WithClientSecret(this.protectedSecretProvider.UnprotectSecret(clientSecret))
                .Build();

            return confidentialClientApplication;
        }

        private IGraphServiceClient GetClient(string tenantId)
        {
            if (!this.clients.TryGetValue(tenantId, out IGraphServiceClient client))
            {
                throw new AadTenantNotFoundException($"The Azure AD tenant ID was not found in the configuration ({tenantId})");
            }

            return client;
        }

        public void AddOrUpdateClientCredentials(string tenantId, string clientId, ProtectedSecret secret)
        {
            if (this.clients.ContainsKey(tenantId))
            {
                this.clients.Remove(tenantId);
            }

            IConfidentialClientApplication confidentialClientApplication = this.BuildConfidentialClientApplication(tenantId, clientId, secret);
            ClientCredentialProvider authProvider = new ClientCredentialProvider(confidentialClientApplication);
            this.clients.Add(tenantId, new GraphServiceClient(authProvider));
        }

        public async Task ValidateCredentials(string tenantId, string clientId, ProtectedSecret secret)
        {
            await this.ValidateCredentials(tenantId, clientId, secret, new string[] { "Device.Read.All", "Group.Read.All", "Organization.Read.All" });
        }

        public async Task<string> GetTenantOrgName(string tenantId)
        {
            return await this.GetTenantOrgName(tenantId, false);
        }

        public async Task<string> GetTenantOrgName(string tenantId, bool forceRefresh)
        {
            string cachedName = this.tenantNameCache.GetValueOrDefault(tenantId, null) ?? this.azureAdOptions.CurrentValue.Tenants.FirstOrDefault(t => string.Equals(t.TenantId, tenantId, StringComparison.OrdinalIgnoreCase))?.TenantName;

            if (cachedName != null && !forceRefresh)
            {
                return cachedName;
            }

            var org = await this.GetClient(tenantId).Organization.Request().GetAsync();
            if (org.Count == 0)
            {
                return null;
            }

            this.tenantNameCache.TryAdd(tenantId, org[0].DisplayName);
            return org[0].DisplayName;
        }

        public async Task ValidateCredentials(string tenantId, string clientId, ProtectedSecret secret, string[] requiredRoles)
        {
            IConfidentialClientApplication confidentialClientApplication = this.BuildConfidentialClientApplication(tenantId, clientId, secret);

            var token = await confidentialClientApplication.AcquireTokenForClient(new string[] { "https://graph.microsoft.com/.default" }).WithForceRefresh(true).ExecuteAsync();

            if (requiredRoles == null)
            {
                return;
            }

            JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
            var securityToken = tokenHandler.ReadToken(token.AccessToken) as JwtSecurityToken;
            List<string> missingRoles = new List<string>();

            var claims = securityToken?.Claims.Where(t => string.Equals(t.Type, "roles", StringComparison.OrdinalIgnoreCase))?.ToList();

            if (claims == null || claims.Count == 0)
            {
                missingRoles.AddRange(requiredRoles);
            }
            else
            {
                foreach (var requiredRole in requiredRoles)
                {
                    if (!claims.Any(t => string.Equals(t.Value, requiredRole, StringComparison.OrdinalIgnoreCase)))
                    {
                        missingRoles.Add(requiredRole);
                    }
                }
            }

            if (missingRoles.Count > 0)
            {
                throw new AadMissingPermissionException($"The application was missing the following API permissions - {string.Join(',', missingRoles)}", missingRoles);
            }
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

        public async IAsyncEnumerable<Group> FindGroups(string tenant, string searchText)
        {
            var pages = await this.GetClient(tenant).Groups
                .Request()
                .Filter($"startswith(displayName,'{searchText}')")
                .Select("id,displayName,securityIdentifier,description")
                .GetAsync();

            foreach (var item in pages.CurrentPage)
            {
                yield return item;
            }

            while (pages.NextPageRequest != null)
            {
                pages = await pages.NextPageRequest.GetAsync();

                foreach (var item in pages.CurrentPage)
                {
                    yield return item;
                }
            }
        }

        public async IAsyncEnumerable<Device> FindDevices(string tenant, string searchText)
        {
            var pages = await this.GetClient(tenant).Devices
                .Request()
                .Filter($"startswith(displayName,'{searchText}')")
                .Select("id,displayName,securityIdentifier,description")
                .GetAsync();

            foreach (var item in pages.CurrentPage)
            {
                yield return item;
            }

            while (pages.NextPageRequest != null)
            {
                pages = await pages.NextPageRequest.GetAsync();

                foreach (var item in pages.CurrentPage)
                {
                    yield return item;
                }
            }
        }

        public async Task<Group> GetAadGroupByIdAsync(string tenant, string objectId)
        {
            try
            {
                return await this.GetClient(tenant).Groups[objectId].Request().GetAsync();
            }
            catch (ServiceException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                throw new AadObjectNotFoundException($"Group with object ID {objectId} was not found in the directory", ex);
            }
        }

        public async Task<Device> GetAadDeviceByIdAsync(string tenant, string objectId)
        {
            try
            {
                return await this.GetClient(tenant).Devices[objectId].Request().GetAsync();
            }
            catch (ServiceException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                throw new AadObjectNotFoundException($"Device with object ID {objectId} was not found in the directory", ex);
            }
        }

        public async Task<Device> GetAadDeviceByDeviceIdAsync(string tenant, string deviceId)
        {
            IGraphServiceDevicesCollectionPage aadDevices = await this.GetClient(tenant).Devices.Request().Filter($"deviceId eq '{WebUtility.UrlEncode(deviceId)}'").GetAsync();

            if (aadDevices.Count == 0)
            {
                throw new AadObjectNotFoundException($"Device with device ID {deviceId} was not found in the directory");
            }

            if (aadDevices.Count > 1)
            {
                throw new AmbiguousNameException($"Multiple objects were found in the directory with the device ID {deviceId}");
            }

            return aadDevices.CurrentPage[0];
        }
    }
}
