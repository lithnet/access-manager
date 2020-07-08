using FluentValidation;

namespace Lithnet.AccessManager.Server.UI
{
    public class PowershellNotificationChannelDefinitionValidator : AbstractValidator<PowershellNotificationChannelDefinitionViewModel>
    {
        public PowershellNotificationChannelDefinitionValidator(INotificationSubscriptionProvider subscriptionProvider, IAppPathProvider appPathProvider)
        {
            this.RuleFor(r => r.DisplayName)
                .NotEmpty().WithMessage("Display name is required")
                .Must((item, propertyValue) => subscriptionProvider.IsUnique(item.DisplayName, item.Id)).WithMessage("The display name is already in use");

            this.RuleFor(r => r.Script)
                .SetValidator(new FileSelectionViewModelValidator(appPathProvider));
        }
    }
}
