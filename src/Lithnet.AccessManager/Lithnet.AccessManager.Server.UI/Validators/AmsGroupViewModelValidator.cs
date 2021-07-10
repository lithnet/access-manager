using System;
using FluentValidation;

namespace Lithnet.AccessManager.Server.UI
{
    public class AmsGroupViewModelValidator : AbstractValidator<AmsGroupViewModel>
    {
        public AmsGroupViewModelValidator()
        {
            this.RuleFor(r => r.Name)
                .NotEmpty()
                .WithMessage("A name must be provided");

        }
    }
}
