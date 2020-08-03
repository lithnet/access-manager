using System.IO;
using FluentValidation;

namespace Lithnet.AccessManager.Server.UI
{
    public class WebhookNotificationChannelDefinitionValidator : AbstractValidator<WebhookNotificationChannelDefinitionViewModel>
    {
        public WebhookNotificationChannelDefinitionValidator(INotificationSubscriptionProvider subscriptionProvider, IAppPathProvider appPathProvider)
        {
            this.RuleFor(r => r.Url).NotEmpty();
            this.RuleFor(r => r.ContentType).NotEmpty();
            this.RuleFor(r => r.HttpMethod).NotEmpty();

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
