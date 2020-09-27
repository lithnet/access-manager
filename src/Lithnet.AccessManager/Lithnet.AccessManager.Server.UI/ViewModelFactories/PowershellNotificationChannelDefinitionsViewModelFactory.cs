using System;
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
        private readonly Func<IEventAggregator> eventAggregator;
        private readonly Func<INotifyModelChangedEventPublisher> eventPublisher;

        public PowershellNotificationChannelDefinitionsViewModelFactory(PowershellNotificationChannelDefinitionViewModelFactory factory, IDialogCoordinator dialogCoordinator, Func<IEventAggregator> eventAggregator, Func<INotifyModelChangedEventPublisher> eventPublisher) : base(factory)
        {
            this.factory = factory;
            this.dialogCoordinator = dialogCoordinator;
            this.eventAggregator = eventAggregator;
            this.eventPublisher = eventPublisher;
        }

        public override NotificationChannelDefinitionsViewModel<PowershellNotificationChannelDefinition, PowershellNotificationChannelDefinitionViewModel> CreateViewModel(IList<PowershellNotificationChannelDefinition> model)
        {
            return new PowershellNotificationChannelDefinitionsViewModel(model, this.factory, this.dialogCoordinator, this.eventAggregator.Invoke(), this.eventPublisher.Invoke());
        }
    }
}
