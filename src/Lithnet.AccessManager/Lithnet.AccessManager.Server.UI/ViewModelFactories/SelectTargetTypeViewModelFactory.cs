using System;
using Lithnet.AccessManager.Api;
using Lithnet.AccessManager.Server.UI.Providers;
using Microsoft.Extensions.Logging;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class SelectTargetTypeViewModelFactory : IViewModelFactory<SelectTargetTypeViewModel>
    {
        private readonly AzureAdOptions azureAdOptions;
        private readonly IRegistryProvider registryProvider;
        private readonly IDomainTrustProvider domainTrustProvider;
        private readonly IViewModelFactory<AzureAdTenantDetailsViewModel, AzureAdTenantDetails> aadTenantViewModelFactory;

        public SelectTargetTypeViewModelFactory(AzureAdOptions azureAdOptions, ApiAuthenticationOptions apiOptions, IViewModelFactory<AzureAdTenantDetailsViewModel, AzureAdTenantDetails> aadTenantViewModelFactory, IDomainTrustProvider domainTrustProvider, IRegistryProvider registryProvider)
        {
            this.azureAdOptions = azureAdOptions;
            this.aadTenantViewModelFactory = aadTenantViewModelFactory;
            this.domainTrustProvider = domainTrustProvider;
            this.registryProvider = registryProvider;
        }

        public SelectTargetTypeViewModel CreateViewModel()
        {
            return new SelectTargetTypeViewModel(azureAdOptions, domainTrustProvider, aadTenantViewModelFactory, registryProvider);
        }
    }
}
