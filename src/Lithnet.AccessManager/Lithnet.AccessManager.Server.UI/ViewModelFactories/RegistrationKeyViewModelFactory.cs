using System;
using Lithnet.AccessManager.Api;
using Lithnet.AccessManager.Server.Providers;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class RegistrationKeyViewModelFactory : IViewModelFactory<RegistrationKeyViewModel, IRegistrationKey>
    {
        private readonly Func<IModelValidator<RegistrationKeyViewModel>> validator;
        private readonly ILogger<RegistrationKeyViewModel> logger;
        private readonly IDialogCoordinator dialogCoordinator;
        private readonly IViewModelFactory<AmsGroupSelectorViewModel> groupSelectorFactory;
        private readonly IRegistrationKeyProvider registrationKeyProvider;
        private readonly IWindowManager windowManager;
        private readonly IViewModelFactory<ExternalDialogWindowViewModel, Screen> externalDialogWindowFactory;

        public RegistrationKeyViewModelFactory(Func<IModelValidator<RegistrationKeyViewModel>> validator, ILogger<RegistrationKeyViewModel> logger, IDialogCoordinator dialogCoordinator, IViewModelFactory<AmsGroupSelectorViewModel> groupSelectorFactory, IRegistrationKeyProvider registrationKeyProvider, IWindowManager windowManager, IViewModelFactory<ExternalDialogWindowViewModel, Screen> externalDialogWindowFactory)
        {
            this.validator = validator;
            this.logger = logger;
            this.dialogCoordinator = dialogCoordinator;
            this.groupSelectorFactory = groupSelectorFactory;
            this.registrationKeyProvider = registrationKeyProvider;
            this.windowManager = windowManager;
            this.externalDialogWindowFactory = externalDialogWindowFactory;
        }

        public RegistrationKeyViewModel CreateViewModel(IRegistrationKey model)
        {
            return new RegistrationKeyViewModel(model, this.logger, this.validator.Invoke(), dialogCoordinator, groupSelectorFactory, registrationKeyProvider, windowManager, externalDialogWindowFactory);
        }
    }
}
