using Lithnet.AccessManager.Server.Configuration;
using Lithnet.AccessManager.Server.UI.Providers;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;

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

        public SecurityDescriptorTargetViewModelFactory(IDialogCoordinator dialogCoordinator, IAppPathProvider appPathProvider, INotificationChannelSelectionViewModelFactory channelSelectionViewModelFactory, IFileSelectionViewModelFactory fileSelectionViewModelFactory, ILogger<SecurityDescriptorTargetViewModel> logger, IDiscoveryServices discoveryServices, IDomainTrustProvider domainTrustProvider, IDirectory directory, ILocalSam localsam)
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
        }

        public SecurityDescriptorTargetViewModel CreateViewModel(SecurityDescriptorTarget model)
        {
            var validator = new SecurityDescriptorTargetViewModelValidator(this.appPathProvider);
            var v = new FluentModelValidator<SecurityDescriptorTargetViewModel>(validator);
            return new SecurityDescriptorTargetViewModel(model, channelSelectionViewModelFactory, fileSelectionViewModelFactory, appPathProvider, logger, dialogCoordinator, v, directory, domainTrustProvider, discoveryServices, localSam);
        }
    }
}
