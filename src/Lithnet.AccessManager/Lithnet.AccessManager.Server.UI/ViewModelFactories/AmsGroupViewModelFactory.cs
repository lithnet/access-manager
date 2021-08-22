using Lithnet.AccessManager.Server.Providers;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;
using Stylet;
using System;

namespace Lithnet.AccessManager.Server.UI
{
    public class AmsGroupViewModelFactory : IViewModelFactory<AmsGroupViewModel, IAmsGroup>
    {
        private readonly Func<IModelValidator<AmsGroupViewModel>> validator;
        private readonly IDialogCoordinator dialogCoordinator;
        private readonly IAmsGroupProvider groupProvider;
        private readonly ILogger<AmsGroupViewModel> logger;
        private readonly IViewModelFactory<AmsDeviceSelectorViewModel> deviceSelectorFactory;
        private readonly IViewModelFactory<ExternalDialogWindowViewModel, Screen> externalDialogWindowFactory;
        private readonly IWindowManager windowManager;

        public AmsGroupViewModelFactory(Func<IModelValidator<AmsGroupViewModel>> validator, IDialogCoordinator dialogCoordinator, IAmsGroupProvider groupProvider, ILogger<AmsGroupViewModel> logger, IViewModelFactory<AmsDeviceSelectorViewModel> deviceSelectorFactory, IViewModelFactory<ExternalDialogWindowViewModel, Screen> externalDialogWindowFactory, IWindowManager windowManager)
        {
            this.validator = validator;
            this.dialogCoordinator = dialogCoordinator;
            this.groupProvider = groupProvider;
            this.logger = logger;
            this.deviceSelectorFactory = deviceSelectorFactory;
            this.externalDialogWindowFactory = externalDialogWindowFactory;
            this.windowManager = windowManager;
        }

        public AmsGroupViewModel CreateViewModel(IAmsGroup model)
        {
            return new AmsGroupViewModel(model, this.validator.Invoke(), groupProvider, dialogCoordinator, logger, deviceSelectorFactory, externalDialogWindowFactory, windowManager);
        }
    }
}
