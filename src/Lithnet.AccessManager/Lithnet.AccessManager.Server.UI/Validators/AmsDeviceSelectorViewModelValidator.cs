using System;
using FluentValidation;

namespace Lithnet.AccessManager.Server.UI
{
    public class AmsDeviceSelectorViewModelValidator : AbstractValidator<AmsDeviceSelectorViewModel>
    {
        public AmsDeviceSelectorViewModelValidator()
        {
            this.RuleFor(r => r.SelectedItem)
                .NotEmpty()
                .WithMessage("An item must be selected");
        }
    }
}
