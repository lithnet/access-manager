using System;
using Lithnet.AccessManager.Api;
using Lithnet.AccessManager.Server.UI.Providers;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class AzureAdObjectSelectorViewModelFactory : IAzureAdObjectSelectorViewModelFactory
    {
        private readonly IDialogCoordinator dialogCoordinator;
        private readonly ILogger<AzureAdObjectSelectorViewModel> logger;
        private readonly IAadGraphApiProvider graphProvider;
        private readonly IModelValidator<AzureAdObjectSelectorViewModel> validator;

        public AzureAdObjectSelectorViewModelFactory(IDialogCoordinator dialogCoordinator, ILogger<AzureAdObjectSelectorViewModel> logger, IAadGraphApiProvider graphProvider, IModelValidator<AzureAdObjectSelectorViewModel> validator)
        {
            this.dialogCoordinator = dialogCoordinator;
            this.logger = logger;
            this.graphProvider = graphProvider;
            this.validator = validator;
        }

        public AzureAdObjectSelectorViewModel CreateViewModel()
        {
            return new AzureAdObjectSelectorViewModel(dialogCoordinator, logger, graphProvider, validator);
        }
    }
}
