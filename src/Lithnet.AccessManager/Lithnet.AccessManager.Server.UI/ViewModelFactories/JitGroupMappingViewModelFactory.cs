using Lithnet.AccessManager.Server.Configuration;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class JitGroupMappingViewModelFactory : IJitGroupMappingViewModelFactory
    {
        private readonly IModelValidator<JitGroupMappingViewModel> validator;
        private readonly ILogger<JitGroupMappingViewModel> logger;
        private readonly IDialogCoordinator dialogCoordinator;
        private readonly IObjectSelectionProvider objectSelectionProvider;
        private readonly IDiscoveryServices discoveryServices;

        public JitGroupMappingViewModelFactory(IModelValidator<JitGroupMappingViewModel> validator, IObjectSelectionProvider objectSelectionProvider, ILogger<JitGroupMappingViewModel> logger, IDialogCoordinator dialogCoordinator, IDiscoveryServices discoveryServices)
        {
            this.objectSelectionProvider = objectSelectionProvider;
            this.validator = validator;
            this.logger = logger;
            this.dialogCoordinator = dialogCoordinator;
            this.discoveryServices = discoveryServices;
        }

        public JitGroupMappingViewModel CreateViewModel(JitGroupMapping model)
        {
            return new JitGroupMappingViewModel(model, logger, dialogCoordinator, validator, discoveryServices, objectSelectionProvider);
        }
    }
}
