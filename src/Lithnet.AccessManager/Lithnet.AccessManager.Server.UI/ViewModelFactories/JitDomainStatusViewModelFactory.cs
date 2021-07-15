using System.DirectoryServices.ActiveDirectory;
using Lithnet.AccessManager.Server.Configuration;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;

namespace Lithnet.AccessManager.Server.UI
{
    public class JitDomainStatusViewModelFactory : IViewModelFactory<JitDomainStatusViewModel, Domain, JitDynamicGroupMapping>
    {
        private readonly IDialogCoordinator dialogCoordinator;
        private readonly IActiveDirectory directory;
        private readonly ILogger<JitDomainStatusViewModel> logger;

        public JitDomainStatusViewModelFactory(IDialogCoordinator dialogCoordinator, IActiveDirectory directory, ILogger<JitDomainStatusViewModel> logger)
        {
            this.dialogCoordinator = dialogCoordinator;
            this.directory = directory;
            this.logger = logger;
        }

        public JitDomainStatusViewModel CreateViewModel(Domain model, JitDynamicGroupMapping mapping)
        {
            return new JitDomainStatusViewModel(directory, mapping, model, logger);
        }
    }
}
