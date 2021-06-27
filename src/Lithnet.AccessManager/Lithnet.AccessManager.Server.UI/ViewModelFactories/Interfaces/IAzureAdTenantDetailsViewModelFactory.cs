using Lithnet.AccessManager.Api;

namespace Lithnet.AccessManager.Server.UI
{
    public interface IAzureAdTenantDetailsViewModelFactory
    {
        AzureAdTenantDetailsViewModel CreateViewModel(AzureAdTenantDetails model);
    }
}