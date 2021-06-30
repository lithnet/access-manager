using System;
using FluentValidation;

namespace Lithnet.AccessManager.Server.UI
{
    public class AzureAdObjectSelectorViewModelValidator : AbstractValidator<AzureAdObjectSelectorViewModel>
    {
        public AzureAdObjectSelectorViewModelValidator()
        {
            this.RuleFor(r => r.SelectedItem)
                .NotEmpty()
                .WithMessage("An item must be selected");
        }
    }
}
