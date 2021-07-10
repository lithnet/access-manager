using System;
using FluentValidation;

namespace Lithnet.AccessManager.Server.UI
{
    public class PasswordPolicyViewModelValidator : AbstractValidator<PasswordPolicyViewModel>
    {
        public PasswordPolicyViewModelValidator()
        {
            this.RuleFor(r => r.PasswordLength)
                .Must(t => t >= 8)
                .WithMessage("The minimum password length must be 8 or more characters");

            this.RuleFor(r => r.MaximumPasswordAgeDays)
                .Must(t => t >= 1)
                .WithMessage("Password age must be at least one day or more");

            this.RuleFor(r => r.UseLower).NotEqual(false)
                .When(r => r.UseNumeric == false)
                .When(r => r.UseSymbol == false)
                .When(r => r.UseUpper == false)
                .WithMessage("At least one character type must be selected");

            this.RuleFor(r => r.UseUpper).NotEqual(false)
                .When(r => r.UseNumeric == false)
                .When(r => r.UseSymbol == false)
                .When(r => r.UseLower == false)
                .WithMessage("At least one character type must be selected");

            this.RuleFor(r => r.UseNumeric).NotEqual(false)
                .When(r => r.UseUpper == false)
                .When(r => r.UseSymbol == false)
                .When(r => r.UseLower == false)
                .WithMessage("At least one character type must be selected");

            this.RuleFor(r => r.UseSymbol).NotEqual(false)
                .When(r => r.UseUpper == false)
                .When(r => r.UseNumeric == false)
                .When(r => r.UseLower == false)
                .WithMessage("At least one character type must be selected");

            this.RuleFor(r => r.Name)
                .NotEmpty()
                .When(r => !r.IsDefault)
                .WithMessage("A policy name must be provided");

            this.RuleFor(r => r.TargetGroup)
                .NotEmpty()
                .When(r => !r.IsDefault)
                .WithMessage("A group must be specified");

            this.RuleFor(r => r.TargetType)
                .NotEqual(AuthorityType.None)
                .When(r => !r.IsDefault)
                .WithMessage("A group type must be specified");
        }
    }
}
