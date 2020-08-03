using System;
using System.Linq.Expressions;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class FileSelectionViewModelFactory : IFileSelectionViewModelFactory
    {
        private readonly IDialogCoordinator dialogCoordinator;
        private readonly IAppPathProvider appPathProvider;
        private readonly IModelValidator<FileSelectionViewModel> validator;
        private readonly ILogger<FileSelectionViewModel> logger;

        public FileSelectionViewModelFactory(IDialogCoordinator dialogCoordinator, IAppPathProvider appPathProvider, IModelValidator<FileSelectionViewModel> validator, ILogger<FileSelectionViewModel> logger)
        {
            this.dialogCoordinator = dialogCoordinator;
            this.appPathProvider = appPathProvider;
            this.validator = validator;
            this.logger = logger;
        }

        public FileSelectionViewModel CreateViewModel(object model, Expression<Func<string>> exp, string basePath)
        {
            return new FileSelectionViewModel(model, exp, basePath, validator, this.dialogCoordinator, this.appPathProvider, this.logger);
        }
    }
}
