using System;
using FluentValidation;

namespace Lithnet.AccessManager.Server.UI
{
    public class RegistrationKeyViewModelValidator : AbstractValidator<RegistrationKeyViewModel>
    {
        public RegistrationKeyViewModelValidator()
        {
            this.RuleFor(r => r.Name)
                .NotEmpty()
                .WithMessage("A name must be provided");

        }
    }
}
