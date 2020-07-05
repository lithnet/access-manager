using System;
using System.Collections.Generic;
using System.Text;
using FluentValidation;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class SmtpNotificationChannelDefinitionValidator : AbstractValidator<SmtpNotificationChannelDefinitionViewModel>
    {
        private readonly INotificationSubscriptionProvider provider;


        public SmtpNotificationChannelDefinitionValidator(INotificationSubscriptionProvider subscriptionProvider)
        {
            this.provider = subscriptionProvider;

            this.RuleFor(r => r.EmailAddresses).Must(t => t?.Count > 0).WithMessage("At least one email address is required");
            this.RuleFor(t => t.NewRecipient).EmailAddress().When(t => t != null).WithMessage("The value must be a valid email address"); 

            this.RuleFor(r => r.DisplayName)
                .NotEmpty().WithMessage("Display name is required")
                .Must((item, propertyValue) => this.provider.IsUnique(item.DisplayName, item.Id)).WithMessage("The display name is already in use");

            this.RuleFor(r => r.TemplateSuccess)
                .NotEmpty().WithMessage("A file must be provided")
                .Must(t => t == null || System.IO.File.Exists(t)).WithMessage("The file does not exist");

            this.RuleFor(r => r.TemplateFailure)
                .NotEmpty().WithMessage("A file must be provided")
                .Must(t => t == null || System.IO.File.Exists(t)).WithMessage("The file does not exist");
        }
    }
}
