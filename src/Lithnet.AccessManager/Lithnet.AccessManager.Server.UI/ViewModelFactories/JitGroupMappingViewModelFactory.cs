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
        private readonly IDirectory directory;
        private readonly IDiscoveryServices discoveryServices;

        public JitGroupMappingViewModelFactory(IModelValidator<JitGroupMappingViewModel> validator, IDirectory directory, ILogger<JitGroupMappingViewModel> logger, IDialogCoordinator dialogCoordinator, IDiscoveryServices discoveryServices)
        {
            this.directory = directory;
            this.validator = validator;
            this.logger = logger;
            this.dialogCoordinator = dialogCoordinator;
            this.discoveryServices = discoveryServices;
        }

        public JitGroupMappingViewModel CreateViewModel(JitGroupMapping model)
        {
            return new JitGroupMappingViewModel(model, directory, logger, dialogCoordinator, validator, discoveryServices);
        }
    }
}
