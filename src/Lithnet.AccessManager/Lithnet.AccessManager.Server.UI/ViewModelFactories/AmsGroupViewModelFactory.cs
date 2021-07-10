using Stylet;
using System;
using Lithnet.AccessManager.Server.Providers;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;

namespace Lithnet.AccessManager.Server.UI
{
    public class AmsGroupViewModelFactory : IViewModelFactory<AmsGroupViewModel, IAmsGroup>
    {
        private readonly Func<IModelValidator<AmsGroupViewModel>> validator;
        private readonly IDialogCoordinator dialogCoordinator;
        private readonly IAmsGroupProvider groupProvider;
        private readonly ILogger<AmsGroupViewModel> logger;
        private readonly IViewModelFactory<AmsDeviceSelectorViewModel> deviceSelectorFactory;

        public AmsGroupViewModelFactory(Func<IModelValidator<AmsGroupViewModel>> validator, IDialogCoordinator dialogCoordinator, IAmsGroupProvider groupProvider, ILogger<AmsGroupViewModel> logger, IViewModelFactory<AmsDeviceSelectorViewModel> deviceSelectorFactory)
        {
            this.validator = validator;
            this.dialogCoordinator = dialogCoordinator;
            this.groupProvider = groupProvider;
            this.logger = logger;
            this.deviceSelectorFactory = deviceSelectorFactory;
        }

        public AmsGroupViewModel CreateViewModel(IAmsGroup model)
        {
            return new AmsGroupViewModel(model, this.validator.Invoke(), groupProvider, dialogCoordinator, logger, deviceSelectorFactory);
        }
    }
}
