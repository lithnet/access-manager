using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FluentValidation;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class PowershellNotificationChannelDefinitionValidator : AbstractValidator<PowershellNotificationChannelDefinitionViewModel>
    {
        private readonly INotificationSubscriptionProvider provider;

        public PowershellNotificationChannelDefinitionValidator(INotificationSubscriptionProvider subscriptionProvider)
        {
            this.provider = subscriptionProvider;

            this.RuleFor(r => r.DisplayName)
                .NotEmpty().WithMessage("Display name is required")
                .Must((item, propertyValue) => this.provider.IsUnique(item.DisplayName, item.Id)).WithMessage("The display name is already in use");

            this.RuleFor(r => r.Script)
                .SetValidator(new FileSelectionViewModelValidator());
        }
    }
}
