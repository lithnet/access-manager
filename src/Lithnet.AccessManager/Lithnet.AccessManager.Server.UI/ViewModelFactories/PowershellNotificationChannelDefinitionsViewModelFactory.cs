using System.Collections.Generic;
using Lithnet.AccessManager.Server.Configuration;
using MahApps.Metro.Controls.Dialogs;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class PowershellNotificationChannelDefinitionsViewModelFactory : NotificationChannelDefinitionsViewModelFactory<PowershellNotificationChannelDefinition, PowershellNotificationChannelDefinitionViewModel>
    {
        private readonly PowershellNotificationChannelDefinitionViewModelFactory factory;

        private readonly IDialogCoordinator dialogCoordinator;
        private readonly IEventAggregator eventAggregator;
        private readonly INotifyModelChangedEventPublisher eventPublisher;

        public PowershellNotificationChannelDefinitionsViewModelFactory(PowershellNotificationChannelDefinitionViewModelFactory factory, IDialogCoordinator dialogCoordinator, IEventAggregator eventAggregator, INotifyModelChangedEventPublisher eventPublisher) : base(factory)
        {
            this.factory = factory;
            this.dialogCoordinator = dialogCoordinator;
            this.eventAggregator = eventAggregator;
            this.eventPublisher = eventPublisher;
        }

        public override NotificationChannelDefinitionsViewModel<PowershellNotificationChannelDefinition, PowershellNotificationChannelDefinitionViewModel> CreateViewModel(IList<PowershellNotificationChannelDefinition> model)
        {
            return new PowershellNotificationChannelDefinitionsViewModel(model, this.factory, this.dialogCoordinator, this.eventAggregator, this.eventPublisher);
        }
    }
}
