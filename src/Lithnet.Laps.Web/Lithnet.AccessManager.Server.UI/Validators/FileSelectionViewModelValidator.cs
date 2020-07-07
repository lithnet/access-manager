using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FluentValidation;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class FileSelectionViewModelValidator : AbstractValidator<FileSelectionViewModel>
    {
        public FileSelectionViewModelValidator()
        {
            this.RuleFor(r => r.File)
               .NotEmpty()
                    .When((item) => item.ShouldValidate)
                    .WithMessage("A file name must be provided")
               .Must((item, propertyValue) => string.IsNullOrWhiteSpace(item.File) || File.Exists(AppPathProvider.GetFullPath(item.File, item.BasePath)))
                    .When((item) => item.ShouldValidate)
                    .WithMessage("The file could not be found");
        }
    }
}
