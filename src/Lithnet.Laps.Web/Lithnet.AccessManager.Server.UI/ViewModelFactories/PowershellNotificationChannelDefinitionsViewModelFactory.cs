using System.Collections.Generic;
using Lithnet.AccessManager.Configuration;
using MahApps.Metro.Controls.Dialogs;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class PowershellNotificationChannelDefinitionsViewModelFactory : NotificationChannelDefinitionsViewModelFactory<PowershellNotificationChannelDefinition, PowershellNotificationChannelDefinitionViewModel>
    {
        private readonly PowershellNotificationChannelDefinitionViewModelFactory factory;

        private readonly IDialogCoordinator dialogCoordinator;
        private readonly IEventAggregator eventAggregator;

        public PowershellNotificationChannelDefinitionsViewModelFactory(PowershellNotificationChannelDefinitionViewModelFactory factory, IDialogCoordinator dialogCoordinator, IEventAggregator eventAggregator) : base(factory)
        {
            this.factory = factory;
            this.dialogCoordinator = dialogCoordinator;
            this.eventAggregator = eventAggregator;
        }

        public override NotificationChannelDefinitionsViewModel<PowershellNotificationChannelDefinition, PowershellNotificationChannelDefinitionViewModel> CreateViewModel(IList<PowershellNotificationChannelDefinition> model)
        {
            return new PowershellNotificationChannelDefinitionsViewModel(model, this.factory, this.dialogCoordinator, this.eventAggregator);
        }
    }
}
