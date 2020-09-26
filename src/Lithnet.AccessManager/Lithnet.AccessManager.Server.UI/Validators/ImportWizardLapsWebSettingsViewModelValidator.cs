using System;
using FluentValidation;
using Markdig.Extensions.Yaml;

namespace Lithnet.AccessManager.Server.UI
{
    public class ImportWizardLapsWebSettingsViewModelValidator : AbstractValidator<ImportWizardLapsWebSettingsViewModel>
    {
        public ImportWizardLapsWebSettingsViewModelValidator()
        {
            this.RuleFor(r => r.ImportFile)
                .NotEmpty()
                .WithMessage("An import file must be selected")
                .Must(path => string.IsNullOrWhiteSpace(path) || System.IO.File.Exists(path))
                .WithMessage("The specified file cannot be found");
        }
    }
}
