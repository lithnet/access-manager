using System;
using System.Linq.Expressions;
using Lithnet.AccessManager.Configuration;
using MahApps.Metro.Controls.Dialogs;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class FileSelectionViewModelFactory : IFileSelectionViewModelFactory
    {
        private readonly IDialogCoordinator dialogCoordinator;
        private readonly IAppPathProvider appPathProvider;
        private readonly IModelValidator<FileSelectionViewModel> validator;

        public FileSelectionViewModelFactory(IDialogCoordinator dialogCoordinator, IAppPathProvider appPathProvider, IModelValidator<FileSelectionViewModel> validator)
        {
            this.dialogCoordinator = dialogCoordinator;
            this.appPathProvider = appPathProvider;
            this.validator = validator;
        }

        public FileSelectionViewModel CreateViewModel(object model, Expression<Func<string>> exp, string basePath)
        {
            return new FileSelectionViewModel(model, exp, basePath, validator, this.dialogCoordinator, this.appPathProvider);
        }
    }
}
