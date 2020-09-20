using System;
using FluentValidation;
using Markdig.Extensions.Yaml;

namespace Lithnet.AccessManager.Server.UI
{
    public class ImportSettingsViewModelValidator : AbstractValidator<ImportSettingsViewModel>
    {
        public ImportSettingsViewModelValidator(IAppPathProvider appPathProvider, IDirectory directory)
        {
            this.RuleFor(r => r.Target)
                .NotEmpty()
                .WithMessage("A target must be selected");

            this.RuleFor(r => r.ImportFile)
                .NotEmpty()
                .When(r => r.ImportTypeFile)
                .WithMessage("An import file must be selected");

            this.RuleFor(r => r.JitGroupDisplayName)
                .NotEmpty()
                .When(t => t.AllowJit)
                .WithMessage("A JIT group must be selected")
                .Must((item, propertyValue) => propertyValue == null ||
                                               propertyValue.Contains("{computerName}", StringComparison.OrdinalIgnoreCase) ||
                                               propertyValue.Contains("%computerName%", StringComparison.OrdinalIgnoreCase) ||
                                               (item.JitAuthorizingGroup?.TryParseAsSid(out _) ?? false) ||
                                               directory.TryGetGroup(propertyValue, out _))
                .When(t => t.AllowJit)
                .WithMessage("Select a group using the group selector, or enter a template containing the '%computerName%' placeholder");

            this.RuleFor(r => r.AllowJit)
                .Must((item, propertyValue) => item.AllowBitlocker || item.AllowJit || item.AllowLaps || item.AllowLapsHistory)
                .WithMessage("At least one access rule type must be selected");

            //this.RuleFor(r => r.AllowBitlocker)
            //    .Must((item, propertyValue) => item.AllowBitlocker || item.AllowJit || item.AllowLaps || item.AllowLapsHistory)
            //    .WithMessage("At least one access rule type must be selected");

            //this.RuleFor(r => r.AllowLaps)
            //    .Must((item, propertyValue) => item.AllowBitlocker || item.AllowJit || item.AllowLaps || item.AllowLapsHistory)
            //    .WithMessage("At least one access rule type must be selected");

            //this.RuleFor(r => r.AllowLapsHistory)
            //    .Must((item, propertyValue) => item.AllowBitlocker || item.AllowJit || item.AllowLaps || item.AllowLapsHistory)
            //    .WithMessage("At least one access rule type must be selected");
        }
    }
}
