using Lithnet.AccessManager.Server.Configuration;

namespace Lithnet.AccessManager.Api
{
    public class AzureAdTenantDetails
    {
        public string ClientId { get; set; }

        public ProtectedSecret ClientSecret { get; set; }

        public string TenantId { get; set; }

        public string TenantName { get; set; }
    }
}