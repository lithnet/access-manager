using System;
using FluentValidation;

namespace Lithnet.AccessManager.Server.UI
{
    public class AzureAdTenantDetailsViewModelValidator : AbstractValidator<AzureAdTenantDetailsViewModel>
    {
        public AzureAdTenantDetailsViewModelValidator()
        {
            this.RuleFor(r => r.ClientId)
                .NotEmpty()
                .WithMessage("A client ID must be provided");

            this.RuleFor(r => r.TenantId)
                .NotEmpty()
                .WithMessage("A tenant ID must be provided");

            this.RuleFor(r => r.ClientSecret)
                .NotEmpty()
                .WithMessage("A client secret must be provided");
        }
    }
}
