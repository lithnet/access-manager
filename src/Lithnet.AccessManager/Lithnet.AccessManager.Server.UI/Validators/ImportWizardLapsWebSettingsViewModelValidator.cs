using System;
using System.IO;
using FluentValidation;
using Markdig.Extensions.Yaml;

namespace Lithnet.AccessManager.Server.UI
{
    public class ImportWizardLapsWebSettingsViewModelValidator : AbstractValidator<ImportWizardLapsWebSettingsViewModel>
    {
        public ImportWizardLapsWebSettingsViewModelValidator(IAppPathProvider appPathProvider)
        {
            this.RuleFor(r => r.ImportFile)
                .NotEmpty()
                .WithMessage("An import file must be selected")
                .Must(path => string.IsNullOrWhiteSpace(path) || System.IO.File.Exists(path))
                .WithMessage("The specified file cannot be found");

            this.RuleFor(r => r.TemplateSuccess)
                .NotEmpty().WithMessage("A file must be provided")
                .When(t => t.ImportNotifications)
                .Must(t => string.IsNullOrWhiteSpace(t) || File.Exists(appPathProvider.GetFullPath(t, appPathProvider.TemplatesPath)))
                .When(t => t.ImportNotifications)
                .WithMessage("The file does not exist");

            this.RuleFor(r => r.TemplateFailure)
                .NotEmpty().WithMessage("A file must be provided")
                .When(t => t.ImportNotifications)
                .Must(t => string.IsNullOrWhiteSpace(t) || File.Exists(appPathProvider.GetFullPath(t, appPathProvider.TemplatesPath)))
                .When(t => t.ImportNotifications)
                .WithMessage("The file does not exist");
        }
    }
}
