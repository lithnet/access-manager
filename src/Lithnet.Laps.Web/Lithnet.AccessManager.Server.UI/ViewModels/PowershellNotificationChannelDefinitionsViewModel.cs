using System.Collections.Generic;
using Lithnet.AccessManager.Server.Configuration;
using MahApps.Metro.Controls.Dialogs;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class PowershellNotificationChannelDefinitionsViewModel : NotificationChannelDefinitionsViewModel<PowershellNotificationChannelDefinition, PowershellNotificationChannelDefinitionViewModel>
    {
        public PowershellNotificationChannelDefinitionsViewModel(IList<PowershellNotificationChannelDefinition> model, PowershellNotificationChannelDefinitionViewModelFactory factory, IDialogCoordinator dialogCoordinator,  IEventAggregator eventAggregator) :
            base (model, factory, dialogCoordinator, eventAggregator)
        {
        }

        public override string DisplayName { get; set; } = "PowerShell";
    }
}
