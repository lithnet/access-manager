using System;
using Lithnet.AccessManager.Api;
using Microsoft.Extensions.Logging;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class AzureAdTenantDetailsViewModelFactory : IViewModelFactory<AzureAdTenantDetailsViewModel, AzureAdTenantDetails>
    {
        private readonly Func<IModelValidator<AzureAdTenantDetailsViewModel>> validator;
        private readonly ILogger<AzureAdTenantDetailsViewModel> logger;
        private readonly IProtectedSecretProvider secretProvider;

        public AzureAdTenantDetailsViewModelFactory(Func<IModelValidator<AzureAdTenantDetailsViewModel>> validator, ILogger<AzureAdTenantDetailsViewModel> logger, IProtectedSecretProvider secretProvider)
        {
            this.validator = validator;
            this.logger = logger;
            this.secretProvider = secretProvider;
        }

        public AzureAdTenantDetailsViewModel CreateViewModel(AzureAdTenantDetails model)
        {
            return new AzureAdTenantDetailsViewModel(model, logger, validator.Invoke(), secretProvider);
        }
    }
}
