using System.Threading;
using Lithnet.AccessManager.Server.Configuration;
using MahApps.Metro.Controls.Dialogs;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class WebhookNotificationChannelDefinitionViewModelFactory : NotificationChannelDefinitionViewModelFactory<WebhookNotificationChannelDefinition, WebhookNotificationChannelDefinitionViewModel>
    {
        private readonly IAppPathProvider appPathProvider;
        private readonly INotificationSubscriptionProvider notificationSubscriptionProvider;
        private readonly IModelValidator<WebhookNotificationChannelDefinitionViewModel> validator;

        public WebhookNotificationChannelDefinitionViewModelFactory(IAppPathProvider appPathProvider, INotificationSubscriptionProvider notificationSubscriptionProvider, IModelValidator<WebhookNotificationChannelDefinitionViewModel> validator)
        {
            this.appPathProvider = appPathProvider;
            this.notificationSubscriptionProvider = notificationSubscriptionProvider;
            this.validator = validator;
        }

        public override WebhookNotificationChannelDefinition CreateModel()
        {
            return new WebhookNotificationChannelDefinition();
        }

        public override WebhookNotificationChannelDefinitionViewModel CreateViewModel(WebhookNotificationChannelDefinition model)
        {
            return new WebhookNotificationChannelDefinitionViewModel(model, validator, notificationSubscriptionProvider, appPathProvider);
        }
    }
}
