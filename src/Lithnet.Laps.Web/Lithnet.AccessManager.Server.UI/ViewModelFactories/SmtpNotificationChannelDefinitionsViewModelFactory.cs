using System.Collections.Generic;
using Lithnet.AccessManager.Server.Configuration;
using MahApps.Metro.Controls.Dialogs;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class SmtpNotificationChannelDefinitionsViewModelFactory : NotificationChannelDefinitionsViewModelFactory<SmtpNotificationChannelDefinition, SmtpNotificationChannelDefinitionViewModel>
    {
        private readonly SmtpNotificationChannelDefinitionViewModelFactory factory;

        private readonly IDialogCoordinator dialogCoordinator;
        private readonly IEventAggregator eventAggregator;

        public SmtpNotificationChannelDefinitionsViewModelFactory(SmtpNotificationChannelDefinitionViewModelFactory factory, IDialogCoordinator dialogCoordinator, IEventAggregator eventAggregator) : base(factory)
        {
            this.factory = factory;
            this.dialogCoordinator = dialogCoordinator;
            this.eventAggregator = eventAggregator;
        }

        public override NotificationChannelDefinitionsViewModel<SmtpNotificationChannelDefinition, SmtpNotificationChannelDefinitionViewModel> CreateViewModel(IList<SmtpNotificationChannelDefinition> model)
        {
            return new SmtpNotificationChannelDefinitionsViewModel(model, this.factory, this.dialogCoordinator, this.eventAggregator);
        }
    }
}
