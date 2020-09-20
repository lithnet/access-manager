using System;
using FluentValidation;
using Markdig.Extensions.Yaml;

namespace Lithnet.AccessManager.Server.UI
{
    public class SecurityDescriptorTargetViewModelValidator : AbstractValidator<SecurityDescriptorTargetViewModel>
    {
        public SecurityDescriptorTargetViewModelValidator(IAppPathProvider appPathProvider)
        {
            this.RuleFor(r => r.Target)
                .NotEmpty()
                .WithMessage("A target must be selected");

            this.RuleFor(r => r.DisplayName)
                .NotEmpty()
                .WithMessage("A target must be selected");

            this.RuleFor(r => r.JitGroupDisplayName)
                .NotEmpty()
                .When(t => t.ShowJitOptions && !t.IsModeScript)
                .WithMessage("A JIT group must be selected")
                .Must((item, propertyValue) => propertyValue == null ||
                                               propertyValue.Contains("{computerName}", StringComparison.OrdinalIgnoreCase) ||
                                               propertyValue.Contains("%computerName%", StringComparison.OrdinalIgnoreCase) ||
                                               (item.JitAuthorizingGroup?.TryParseAsSid(out _) ?? false))
                .When(t => t.ShowJitOptions && !t.IsModeScript)
                .WithMessage("Select a group using the group selector, or enter a templated name containing the '%computerName%' placeholder");

            this.RuleFor(r => r.Script)
                .SetValidator(new FileSelectionViewModelValidator(appPathProvider))
                .When(t => t.IsModeScript);
        }
    }
}
