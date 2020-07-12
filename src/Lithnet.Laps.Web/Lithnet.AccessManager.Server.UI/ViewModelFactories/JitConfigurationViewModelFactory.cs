using Lithnet.AccessManager.Server.Configuration;
using MahApps.Metro.Controls.Dialogs;

namespace Lithnet.AccessManager.Server.UI
{
    public class JitConfigurationViewModelFactory : IJitConfigurationViewModelFactory
    {
        private readonly JitConfigurationOptions jitOptions;
        private readonly IDialogCoordinator dialogCoordinator;
        private readonly IDirectory directory;
        private readonly IJitGroupMappingViewModelFactory groupFactory;

        public JitConfigurationViewModelFactory(JitConfigurationOptions jitOptions, IDialogCoordinator dialogCoordinator, IDirectory directory, IJitGroupMappingViewModelFactory groupFactory)
        {
            this.jitOptions = jitOptions;
            this.dialogCoordinator = dialogCoordinator;
            this.directory = directory;
            this.groupFactory = groupFactory;
        }

        public JitConfigurationViewModel CreateViewModel()
        {
            return new JitConfigurationViewModel(jitOptions, dialogCoordinator, directory, groupFactory);
        }
    }
}
