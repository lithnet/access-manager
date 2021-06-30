using System;
using Lithnet.AccessManager.Api;
using Lithnet.AccessManager.Server.UI.Providers;
using Microsoft.Extensions.Logging;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class SelectTargetTypeViewModelFactory : ISelectTargetTypeViewModelFactory
    {
        private readonly AzureAdOptions azureAdOptions;
        private readonly ApiAuthenticationOptions apiOptions;
        private readonly IDomainTrustProvider domainTrustProvider;
        private readonly IAzureAdTenantDetailsViewModelFactory aadTenantViewModelFactory;

        public SelectTargetTypeViewModelFactory(AzureAdOptions azureAdOptions, ApiAuthenticationOptions apiOptions, IAzureAdTenantDetailsViewModelFactory aadTenantViewModelFactory, IDomainTrustProvider domainTrustProvider)
        {
            this.azureAdOptions = azureAdOptions;
            this.apiOptions = apiOptions;
            this.aadTenantViewModelFactory = aadTenantViewModelFactory;
            this.domainTrustProvider = domainTrustProvider;
        }

        public SelectTargetTypeViewModel CreateViewModel()
        {
            return new SelectTargetTypeViewModel(azureAdOptions, apiOptions, domainTrustProvider, aadTenantViewModelFactory);
        }
    }
}
