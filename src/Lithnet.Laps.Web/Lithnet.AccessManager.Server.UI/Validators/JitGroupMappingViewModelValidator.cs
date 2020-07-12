using System;
using FluentValidation;

namespace Lithnet.AccessManager.Server.UI
{
    public class JitGroupMappingViewModelValidator : AbstractValidator<JitGroupMappingViewModel>
    {
        public JitGroupMappingViewModelValidator()
        {
            this.RuleFor(r => r.GroupNameTemplate)
               .NotEmpty()
                    .WithMessage("A group name template must be provided")
               .Must((item, propertyValue) => propertyValue.Contains("{computerName}", StringComparison.OrdinalIgnoreCase))
                    .WithMessage("The template must contain the '{computerName}' placeholder");

            this.RuleFor(r => r.GroupOU)
                .NotEmpty()
                .WithMessage("Select the OU to create group objects in");

            this.RuleFor(r => r.ComputerOU)
                .NotEmpty()
                .WithMessage("Select the OU that contains the computer objects to create groups for");
        }
    }
}
