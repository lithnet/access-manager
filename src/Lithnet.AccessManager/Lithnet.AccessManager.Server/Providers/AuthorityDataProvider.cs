using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Server.Providers
{
    public class AuthorityDataProvider : IAuthorityDataProvider
    {
        private readonly IDiscoveryServices discoveryServices;
        private readonly ILogger<ComputerSearchResultProvider> logger;
        private readonly IAadGraphApiProvider aadGraphProvider;

        public AuthorityDataProvider(ILogger<ComputerSearchResultProvider> logger, IDiscoveryServices discoveryServices, IAadGraphApiProvider aadGraphProvider)
        {
            this.logger = logger;
            this.discoveryServices = discoveryServices;
            this.aadGraphProvider = aadGraphProvider;
        }

        public async Task<string> GetAuthorityName(IComputer item)
        {
            try
            {
                if (item.AuthorityType == AuthorityType.Ams)
                {
                    return item.AuthorityType.ToDescription();
                }
                if (item.AuthorityType == AuthorityType.ActiveDirectory)
                {
                    return this.discoveryServices.GetDomainNameDns(item.SecurityIdentifier);
                }
                else if (item.AuthorityType == AuthorityType.AzureActiveDirectory)
                {
                    string tenantName = await this.aadGraphProvider.GetTenantOrgName(item.AuthorityId);

                    if (tenantName != null)
                    {
                        return tenantName;
                    }
                    else
                    {
                        return $"Unknown tenant ({item.AuthorityId})";
                    }
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Unable to determine authority name");
            }

            return "Unknown authority";
        }

        public async Task<string> GetAuthorityNameAndType(IComputer item)
        {
            var name = await this.GetAuthorityName(item);

            return $"{name} ({item.AuthorityType.ToDescription()})";
        }
    }
}
