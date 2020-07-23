using System.DirectoryServices.ActiveDirectory;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;

namespace Lithnet.AccessManager.Server.UI
{
    public class ActiveDirectoryForestConfigurationViewModelFactory : IActiveDirectoryForestConfigurationViewModelFactory
    {
        private readonly IActiveDirectoryDomainConfigurationViewModelFactory domainFactory;

        private readonly IDirectory directory;

        private readonly IDialogCoordinator dialogCoordinator;

        private readonly ILogger<ActiveDirectoryForestConfigurationViewModel> logger;

        public ActiveDirectoryForestConfigurationViewModelFactory(ICertificateProvider certificateProvider, IActiveDirectoryDomainConfigurationViewModelFactory domainFactory, IDirectory directory, IX509Certificate2ViewModelFactory certificate2ViewModelFactory, IDialogCoordinator dialogCoordinator, ILogger<ActiveDirectoryForestConfigurationViewModel> logger)
        {
            this.domainFactory = domainFactory;
            this.directory = directory;
            this.dialogCoordinator = dialogCoordinator;
            this.logger = logger;
        }

        public ActiveDirectoryForestConfigurationViewModel CreateViewModel(Forest model)
        {
            return new ActiveDirectoryForestConfigurationViewModel(model, dialogCoordinator, domainFactory, directory, logger);
        }
    }
}
