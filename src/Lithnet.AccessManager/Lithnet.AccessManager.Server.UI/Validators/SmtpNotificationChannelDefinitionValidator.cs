using System.IO;
using FluentValidation;

namespace Lithnet.AccessManager.Server.UI
{
    public class SmtpNotificationChannelDefinitionValidator : AbstractValidator<SmtpNotificationChannelDefinitionViewModel>
    {

        public SmtpNotificationChannelDefinitionValidator(INotificationSubscriptionProvider subscriptionProvider, IAppPathProvider appPathProvider)
        {
            this.RuleFor(r => r.EmailAddresses).Must(t => t?.Count > 0).WithMessage("At least one email address is required");
            this.RuleFor(t => t.NewRecipient).EmailAddress().When(t => t != null).WithMessage("The value must be a valid email address"); 

            this.RuleFor(r => r.DisplayName)
                .NotEmpty().WithMessage("Display name is required")
                .Must((item, propertyValue) => subscriptionProvider.IsUnique(item.DisplayName, item.Id)).WithMessage("The display name is already in use");

            this.RuleFor(r => r.TemplateSuccess)
                .NotEmpty().WithMessage("A file must be provided")
                .Must(t => string.IsNullOrWhiteSpace(t) || File.Exists(appPathProvider.GetFullPath(t, appPathProvider.TemplatesPath))).WithMessage("The file does not exist");

            this.RuleFor(r => r.TemplateFailure)
                .NotEmpty().WithMessage("A file must be provided")
                .Must(t => string.IsNullOrWhiteSpace(t) || File.Exists(appPathProvider.GetFullPath(t, appPathProvider.TemplatesPath))).WithMessage("The file does not exist");
        }
    }
}