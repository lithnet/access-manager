using System;
using Lithnet.AccessManager.Api;
using Lithnet.AccessManager.Server.Providers;
using Lithnet.AccessManager.Server.UI.Providers;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class AmsGroupSelectorViewModelFactory : IViewModelFactory<AmsGroupSelectorViewModel>
    {
        private readonly IDialogCoordinator dialogCoordinator;
        private readonly ILogger<AmsGroupSelectorViewModel> logger;
        private readonly IAmsGroupProvider groupProvider;
        private readonly IModelValidator<AmsGroupSelectorViewModel> validator;
        private readonly IViewModelFactory<AmsGroupViewModel, IAmsGroup> factory;

        public AmsGroupSelectorViewModelFactory(IDialogCoordinator dialogCoordinator, ILogger<AmsGroupSelectorViewModel> logger, IAadGraphApiProvider graphProvider, IModelValidator<AmsGroupSelectorViewModel> validator, IAmsGroupProvider groupProvider, IViewModelFactory<AmsGroupViewModel, IAmsGroup> factory)
        {
            this.dialogCoordinator = dialogCoordinator;
            this.logger = logger;
            this.validator = validator;
            this.groupProvider = groupProvider;
            this.factory = factory;
        }

        public AmsGroupSelectorViewModel CreateViewModel()
        {
            return new AmsGroupSelectorViewModel(dialogCoordinator, logger, validator, groupProvider, factory);
        }
    }
}
