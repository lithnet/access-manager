using System.IO;
using FluentValidation;

namespace Lithnet.AccessManager.Server.UI
{
    public class FileSelectionViewModelValidator : AbstractValidator<FileSelectionViewModel>
    {
        public FileSelectionViewModelValidator(IAppPathProvider appPathProvider)
        {
            this.RuleFor(r => r.File)
               .NotEmpty()
                    .When((item) => item.ShouldValidate)
                    .WithMessage("A file name must be provided")
               .Must((item, propertyValue) => string.IsNullOrWhiteSpace(item.File) || File.Exists(appPathProvider.GetFullPath(item.File, item.BasePath)))
                    .When((item) => item.ShouldValidate)
                    .WithMessage("The file could not be found");
        }
    }
}
