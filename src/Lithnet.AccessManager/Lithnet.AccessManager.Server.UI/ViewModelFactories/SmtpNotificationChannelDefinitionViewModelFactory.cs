using System;
using Lithnet.AccessManager.Server.Configuration;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class SmtpNotificationChannelDefinitionViewModelFactory : NotificationChannelDefinitionViewModelFactory<SmtpNotificationChannelDefinition, SmtpNotificationChannelDefinitionViewModel>
    {
        private readonly IAppPathProvider appPathProvider;
        private readonly Func<IModelValidator<SmtpNotificationChannelDefinitionViewModel>> validator;

        public SmtpNotificationChannelDefinitionViewModelFactory(IAppPathProvider appPathProvider, Func<IModelValidator<SmtpNotificationChannelDefinitionViewModel>> validator)
        {
            this.appPathProvider = appPathProvider;
            this.validator = validator;
        }

        public override SmtpNotificationChannelDefinition CreateModel()
        {
            return new SmtpNotificationChannelDefinition();
        }

        public override SmtpNotificationChannelDefinitionViewModel CreateViewModel(SmtpNotificationChannelDefinition model)
        {
            return new SmtpNotificationChannelDefinitionViewModel(model, validator.Invoke(), appPathProvider);
        }
    }
}
