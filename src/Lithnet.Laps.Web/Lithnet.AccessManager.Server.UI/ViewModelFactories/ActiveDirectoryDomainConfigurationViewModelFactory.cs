using System.DirectoryServices.ActiveDirectory;
using MahApps.Metro.Controls.Dialogs;

namespace Lithnet.AccessManager.Server.UI
{
    public class ActiveDirectoryDomainConfigurationViewModelFactory : IActiveDirectoryDomainConfigurationViewModelFactory
    {
        private readonly IDialogCoordinator dialogCoordinator;
        private readonly IDirectory directory;
        private readonly IServiceSettingsProvider serviceSettings;


        public ActiveDirectoryDomainConfigurationViewModelFactory(IDialogCoordinator dialogCoordinator, IDirectory directory, IServiceSettingsProvider serviceSettings)
        {
            this.dialogCoordinator = dialogCoordinator;
            this.directory = directory;
            this.serviceSettings = serviceSettings;
        }

        public ActiveDirectoryDomainConfigurationViewModel CreateViewModel(Domain model)
        {
            return new ActiveDirectoryDomainConfigurationViewModel(model, serviceSettings, directory, dialogCoordinator);
        }
    }
}
