using Lithnet.AccessManager.Server.Configuration;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class SmtpNotificationChannelDefinitionViewModelFactory : NotificationChannelDefinitionViewModelFactory<SmtpNotificationChannelDefinition, SmtpNotificationChannelDefinitionViewModel>
    {
        private readonly IAppPathProvider appPathProvider;
        private readonly INotificationSubscriptionProvider notificationSubscriptionProvider;
        private readonly IModelValidator<SmtpNotificationChannelDefinitionViewModel> validator;

        public SmtpNotificationChannelDefinitionViewModelFactory(IAppPathProvider appPathProvider, INotificationSubscriptionProvider notificationSubscriptionProvider, IModelValidator<SmtpNotificationChannelDefinitionViewModel> validator)
        {
            this.appPathProvider = appPathProvider;
            this.notificationSubscriptionProvider = notificationSubscriptionProvider;
            this.validator = validator;
        }

        public override SmtpNotificationChannelDefinition CreateModel()
        {
            return new SmtpNotificationChannelDefinition();
        }

        public override SmtpNotificationChannelDefinitionViewModel CreateViewModel(SmtpNotificationChannelDefinition model)
        {
            return new SmtpNotificationChannelDefinitionViewModel(model, validator, notificationSubscriptionProvider, appPathProvider);
        }
    }
}
