using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;
using Stylet;
using System;

namespace Lithnet.AccessManager.Server.UI
{
    public class AzureAdObjectSelectorViewModelFactory : IViewModelFactory<AzureAdObjectSelectorViewModel>
    {
        private readonly IDialogCoordinator dialogCoordinator;
        private readonly ILogger<AzureAdObjectSelectorViewModel> logger;
        private readonly IAadGraphApiProvider graphProvider;
        private readonly Func<IModelValidator<AzureAdObjectSelectorViewModel>> validator;

        public AzureAdObjectSelectorViewModelFactory(IDialogCoordinator dialogCoordinator, ILogger<AzureAdObjectSelectorViewModel> logger, IAadGraphApiProvider graphProvider, Func<IModelValidator<AzureAdObjectSelectorViewModel>> validator)
        {
            this.dialogCoordinator = dialogCoordinator;
            this.logger = logger;
            this.graphProvider = graphProvider;
            this.validator = validator;
        }

        public AzureAdObjectSelectorViewModel CreateViewModel()
        {
            return new AzureAdObjectSelectorViewModel(dialogCoordinator, logger, graphProvider, validator.Invoke());
        }
    }
}
