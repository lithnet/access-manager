using System;
using Lithnet.AccessManager.Server.Configuration;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class WebhookNotificationChannelDefinitionViewModelFactory : NotificationChannelDefinitionViewModelFactory<WebhookNotificationChannelDefinition, WebhookNotificationChannelDefinitionViewModel>
    {
        private readonly IAppPathProvider appPathProvider;
        private readonly Func<IModelValidator<WebhookNotificationChannelDefinitionViewModel>> validator;

        public WebhookNotificationChannelDefinitionViewModelFactory(IAppPathProvider appPathProvider, Func<IModelValidator<WebhookNotificationChannelDefinitionViewModel>> validator)
        {
            this.appPathProvider = appPathProvider;
            this.validator = validator;
        }

        public override WebhookNotificationChannelDefinition CreateModel()
        {
            return new WebhookNotificationChannelDefinition();
        }

        public override WebhookNotificationChannelDefinitionViewModel CreateViewModel(WebhookNotificationChannelDefinition model)
        {
            return new WebhookNotificationChannelDefinitionViewModel(model, validator.Invoke(), appPathProvider);
        }
    }
}
