using Lithnet.AccessManager.Server.Configuration;
using Lithnet.AccessManager.Server.UI.Providers;
using Lithnet.AccessManager.Server.UI.ViewModels;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class ImportTargetsViewModelFactory : IImportTargetsViewModelFactory
    {
        private readonly IDialogCoordinator dialogCoordinator;
        private readonly IAppPathProvider appPathProvider;
        private readonly INotificationChannelSelectionViewModelFactory channelSelectionViewModelFactory;
        private readonly IFileSelectionViewModelFactory fileSelectionViewModelFactory;
        private readonly ILogger<ImportSettingsViewModel> logger;
        private readonly IModelValidator<ImportSettingsViewModel> validator;
        private readonly IDiscoveryServices discoveryServices;
        private readonly IDirectory directory;
        private readonly IDomainTrustProvider domainTrustProvider;
        private readonly IShellExecuteProvider shellExecuteProvider;

        public ImportTargetsViewModelFactory(IDialogCoordinator dialogCoordinator, IAppPathProvider appPathProvider, INotificationChannelSelectionViewModelFactory channelSelectionViewModelFactory, IFileSelectionViewModelFactory fileSelectionViewModelFactory, ILogger<ImportSettingsViewModel> logger, IModelValidator<ImportSettingsViewModel> validator, IDiscoveryServices discoveryServices, IDomainTrustProvider domainTrustProvider, IDirectory directory, IShellExecuteProvider shellExecuteProvider)
        {
            this.dialogCoordinator = dialogCoordinator;
            this.appPathProvider = appPathProvider;
            this.channelSelectionViewModelFactory = channelSelectionViewModelFactory;
            this.fileSelectionViewModelFactory = fileSelectionViewModelFactory;
            this.logger = logger;
            this.validator = validator;
            this.directory = directory;
            this.discoveryServices = discoveryServices;
            this.domainTrustProvider = domainTrustProvider;
            this.shellExecuteProvider = shellExecuteProvider;
        }

        public ImportSettingsViewModel CreateViewModel()
        {
            return new ImportSettingsViewModel(channelSelectionViewModelFactory, fileSelectionViewModelFactory, appPathProvider, logger, dialogCoordinator, validator, directory, domainTrustProvider, discoveryServices, shellExecuteProvider );
        }
    }
}
