using System;
using FluentValidation;
using Markdig.Extensions.Yaml;

namespace Lithnet.AccessManager.Server.UI
{
    public class ImportWizardImportContainerViewModelValidator : AbstractValidator<ImportWizardImportContainerViewModel>
    {
        public ImportWizardImportContainerViewModelValidator()
        {
            this.RuleFor(r => r.Target)
                .NotEmpty()
                .WithMessage("A container must be selected");
        }
    }
}
