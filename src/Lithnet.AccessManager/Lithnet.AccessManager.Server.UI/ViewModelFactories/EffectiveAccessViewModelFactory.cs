using Lithnet.AccessManager.Server.Authorization;
using Lithnet.AccessManager.Server.Providers;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;
using Stylet;
using System.Collections.Generic;

namespace Lithnet.AccessManager.Server.UI
{
    public class EffectiveAccessViewModelFactory : IViewModelFactory<EffectiveAccessViewModel, SecurityDescriptorTargetsViewModel>
    {
        private readonly IDialogCoordinator dialogCoordinator;
        private readonly ILogger<EffectiveAccessViewModel> logger;
        private readonly IAuthorizationInformationBuilder authorizationBuilder;
        private readonly IActiveDirectory directory;
        private readonly IEnumerable<IComputerTargetProvider> computerTargetProvider;
        private readonly IComputerLocator computerLocator;
        private readonly IAsyncViewModelFactory<ComputerSelectorViewModel, IList<IComputer>> computerSelectorViewModelFactory;
        private readonly IWindowManager windowManager;
        private readonly IViewModelFactory<ExternalDialogWindowViewModel, Screen> externalDialogWindowFactory;
        public EffectiveAccessViewModelFactory(IDialogCoordinator dialogCoordinator, ILogger<EffectiveAccessViewModel> logger, IAuthorizationInformationBuilder authorizationBuilder, IActiveDirectory directory, IEnumerable<IComputerTargetProvider> computerTargetProvider, IComputerLocator computerLocator, IAsyncViewModelFactory<ComputerSelectorViewModel, IList<IComputer>> computerSelectorViewModelFactory, IWindowManager windowManager, IViewModelFactory<ExternalDialogWindowViewModel, Screen> externalDialogWindowFactory)
        {
            this.dialogCoordinator = dialogCoordinator;
            this.logger = logger;
            this.directory = directory;
            this.authorizationBuilder = authorizationBuilder;
            this.computerTargetProvider = computerTargetProvider;
            this.computerLocator = computerLocator;
            this.computerSelectorViewModelFactory = computerSelectorViewModelFactory;
            this.windowManager = windowManager;
            this.externalDialogWindowFactory = externalDialogWindowFactory;
        }

        public EffectiveAccessViewModel CreateViewModel(SecurityDescriptorTargetsViewModel targets)
        {
            return new EffectiveAccessViewModel(authorizationBuilder, dialogCoordinator, directory, targets, logger, computerTargetProvider, computerLocator, computerSelectorViewModelFactory, externalDialogWindowFactory, windowManager);
        }
    }
}
