using System;
using System.Threading.Tasks;
using Lithnet.AccessManager.Server.Configuration;
using Lithnet.AccessManager.Server.UI.Providers;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class SecurityDescriptorTargetViewModelFactory : ISecurityDescriptorTargetViewModelFactory
    {
        private readonly IDialogCoordinator dialogCoordinator;
        private readonly IAppPathProvider appPathProvider;
        private readonly INotificationChannelSelectionViewModelFactory channelSelectionViewModelFactory;
        private readonly IFileSelectionViewModelFactory fileSelectionViewModelFactory;
        private readonly ILogger<SecurityDescriptorTargetViewModel> logger;
        private readonly IDiscoveryServices discoveryServices;
        private readonly IDirectory directory;
        private readonly IDomainTrustProvider domainTrustProvider;
        private readonly ILocalSam localSam;
        private readonly IObjectSelectionProvider objectSelectionProvider;
        private readonly Func<IModelValidator<SecurityDescriptorTargetViewModel>> validator;

        public SecurityDescriptorTargetViewModelFactory(IDialogCoordinator dialogCoordinator, IAppPathProvider appPathProvider, INotificationChannelSelectionViewModelFactory channelSelectionViewModelFactory, IFileSelectionViewModelFactory fileSelectionViewModelFactory, ILogger<SecurityDescriptorTargetViewModel> logger, IDiscoveryServices discoveryServices, IDomainTrustProvider domainTrustProvider, IDirectory directory, ILocalSam localsam, IObjectSelectionProvider objectSelectionProvider, Func<IModelValidator<SecurityDescriptorTargetViewModel>> validator)
        {
            this.dialogCoordinator = dialogCoordinator;
            this.appPathProvider = appPathProvider;
            this.channelSelectionViewModelFactory = channelSelectionViewModelFactory;
            this.fileSelectionViewModelFactory = fileSelectionViewModelFactory;
            this.logger = logger;
            this.directory = directory;
            this.discoveryServices = discoveryServices;
            this.domainTrustProvider = domainTrustProvider;
            this.localSam = localsam;
            this.objectSelectionProvider = objectSelectionProvider;
            this.validator = validator;
        }

        public async Task<SecurityDescriptorTargetViewModel> CreateViewModelAsync(SecurityDescriptorTarget model, SecurityDescriptorTargetViewModelDisplaySettings settings)
        {

            var item = new SecurityDescriptorTargetViewModel(model, settings, channelSelectionViewModelFactory, fileSelectionViewModelFactory, appPathProvider, logger, dialogCoordinator, validator.Invoke(), directory, domainTrustProvider, discoveryServices, localSam, objectSelectionProvider);

            await item.Initialization;

            return item;
        }
    }
}
