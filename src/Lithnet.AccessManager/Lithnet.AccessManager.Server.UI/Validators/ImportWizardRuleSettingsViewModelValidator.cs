using System;
using FluentValidation;

namespace Lithnet.AccessManager.Server.UI
{
    public class ImportWizardRuleSettingsViewModelValidator : AbstractValidator<ImportWizardRuleSettingsViewModel>
    {
        public ImportWizardRuleSettingsViewModelValidator(IAppPathProvider appPathProvider, IDirectory directory)
        {
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

            this.RuleFor(r => r.AllowLaps)
                .Must((item, propertyValue) => item.AllowBitlocker || item.AllowJit || item.AllowLaps || item.AllowLapsHistory)
                .WithMessage("At least one access rule type must be selected");
        }
    }
}
