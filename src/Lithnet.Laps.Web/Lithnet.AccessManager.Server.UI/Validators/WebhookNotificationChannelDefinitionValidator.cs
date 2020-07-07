using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FluentValidation;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class WebhookNotificationChannelDefinitionValidator : AbstractValidator<WebhookNotificationChannelDefinitionViewModel>
    {
        private readonly INotificationSubscriptionProvider provider;

        public WebhookNotificationChannelDefinitionValidator(INotificationSubscriptionProvider subscriptionProvider)
        {
            this.provider = subscriptionProvider;
            
            this.RuleFor(r => r.Url).NotEmpty();
            this.RuleFor(r => r.ContentType).NotEmpty();
            this.RuleFor(r => r.HttpMethod).NotEmpty();

            this.RuleFor(r => r.DisplayName)
                .NotEmpty().WithMessage("Display name is required")
                .Must((item, propertyValue) => this.provider.IsUnique(item.DisplayName, item.Id)).WithMessage("The display name is already in use");

            this.RuleFor(r => r.TemplateSuccess)
                .NotEmpty().WithMessage("A file must be provided")
                .Must(t => string.IsNullOrWhiteSpace(t) || File.Exists(AppPathProvider.GetFullPath(t, AppPathProvider.TemplatesPath))).WithMessage("The file does not exist");

            this.RuleFor(r => r.TemplateFailure)
                .NotEmpty().WithMessage("A file must be provided")
                .Must(t => string.IsNullOrWhiteSpace(t) || File.Exists(AppPathProvider.GetFullPath(t, AppPathProvider.TemplatesPath))).WithMessage("The file does not exist");
        }
    }
}
