using System;
using FluentValidation;

namespace Lithnet.AccessManager.Server.UI
{
    public class AmsGroupSelectorViewModelValidator : AbstractValidator<AmsGroupSelectorViewModel>
    {
        public AmsGroupSelectorViewModelValidator()
        {
            this.RuleFor(r => r.SelectedItem)
                .NotEmpty()
                .WithMessage("An item must be selected");
        }
    }
}
