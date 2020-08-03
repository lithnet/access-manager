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
        private readonly INotifiableEventPublisher eventPublisher;

        public SmtpNotificationChannelDefinitionsViewModelFactory(SmtpNotificationChannelDefinitionViewModelFactory factory, IDialogCoordinator dialogCoordinator, IEventAggregator eventAggregator, INotifiableEventPublisher eventPublisher) : base(factory)
        {
            this.factory = factory;
            this.dialogCoordinator = dialogCoordinator;
            this.eventAggregator = eventAggregator;
            this.eventPublisher = eventPublisher;
        }

        public override NotificationChannelDefinitionsViewModel<SmtpNotificationChannelDefinition, SmtpNotificationChannelDefinitionViewModel> CreateViewModel(IList<SmtpNotificationChannelDefinition> model)
        {
            return new SmtpNotificationChannelDefinitionsViewModel(model, this.factory, this.dialogCoordinator, this.eventAggregator, this.eventPublisher);
        }
    }
}
