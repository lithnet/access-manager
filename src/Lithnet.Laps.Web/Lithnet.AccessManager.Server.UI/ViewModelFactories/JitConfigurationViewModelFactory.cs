using System;
using System.DirectoryServices.ActiveDirectory;
using System.Linq.Expressions;
using Lithnet.AccessManager.Server.Configuration;
using MahApps.Metro.Controls.Dialogs;
using Stylet;

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
