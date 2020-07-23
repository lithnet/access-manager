using Lithnet.AccessManager.Server.Configuration;
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

        public SecurityDescriptorTargetViewModelFactory(IDialogCoordinator dialogCoordinator, IAppPathProvider appPathProvider, INotificationChannelSelectionViewModelFactory channelSelectionViewModelFactory, IFileSelectionViewModelFactory fileSelectionViewModelFactory, ILogger<SecurityDescriptorTargetViewModel> logger)
        {
            this.dialogCoordinator = dialogCoordinator;
            this.appPathProvider = appPathProvider;
            this.channelSelectionViewModelFactory = channelSelectionViewModelFactory;
            this.fileSelectionViewModelFactory = fileSelectionViewModelFactory;
            this.logger = logger;
        }

        public SecurityDescriptorTargetViewModel CreateViewModel(SecurityDescriptorTarget model)
        {
            return new SecurityDescriptorTargetViewModel(model, channelSelectionViewModelFactory, fileSelectionViewModelFactory, appPathProvider, logger, dialogCoordinator);
        }
    }
}
