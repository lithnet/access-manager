using System;
using FluentValidation;

namespace Lithnet.AccessManager.Server.UI
{
    public class ComputerSelectorViewModelValidator : AbstractValidator<ComputerSelectorViewModel>
    {
        public ComputerSelectorViewModelValidator()
        {
            this.RuleFor(r => r.SelectedItem)
                .NotEmpty()
                .WithMessage("An item must be selected");
        }
    }
}
