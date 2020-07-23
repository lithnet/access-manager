using System.DirectoryServices.ActiveDirectory;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;

namespace Lithnet.AccessManager.Server.UI
{
    public class ActiveDirectoryDomainConfigurationViewModelFactory : IActiveDirectoryDomainConfigurationViewModelFactory
    {
        private readonly IDialogCoordinator dialogCoordinator;
        private readonly IDirectory directory;
        private readonly IServiceSettingsProvider serviceSettings;
        private readonly ILogger<ActiveDirectoryDomainConfigurationViewModel> logger;

        public ActiveDirectoryDomainConfigurationViewModelFactory(IDialogCoordinator dialogCoordinator, IDirectory directory, IServiceSettingsProvider serviceSettings, ILogger<ActiveDirectoryDomainConfigurationViewModel> logger)
        {
            this.dialogCoordinator = dialogCoordinator;
            this.directory = directory;
            this.serviceSettings = serviceSettings;
            this.logger = logger;
        }

        public ActiveDirectoryDomainConfigurationViewModel CreateViewModel(Domain model)
        {
            return new ActiveDirectoryDomainConfigurationViewModel(model, serviceSettings, directory, dialogCoordinator, logger);
        }
    }
}
