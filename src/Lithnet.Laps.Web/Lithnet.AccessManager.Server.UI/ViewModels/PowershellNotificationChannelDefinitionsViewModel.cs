using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;
using Lithnet.AccessManager.Configuration;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.SimpleChildWindow;
using Newtonsoft.Json;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class PowershellNotificationChannelDefinitionsViewModel : NotificationChannelDefinitionsViewModel<PowershellNotificationChannelDefinition, PowershellNotificationChannelDefinitionViewModel>
    {
        public PowershellNotificationChannelDefinitionsViewModel(IList<PowershellNotificationChannelDefinition> model, IDialogCoordinator dialogCoordinator, INotificationSubscriptionProvider subscriptionProvider, IEventAggregator eventAggregator) :
            base (model, dialogCoordinator, subscriptionProvider, eventAggregator)
        {
        }

        protected override PowershellNotificationChannelDefinitionViewModel CreateViewModel(PowershellNotificationChannelDefinition model)
        {
            return new PowershellNotificationChannelDefinitionViewModel(model, this.NotificationSubscriptions);
        }

        protected override PowershellNotificationChannelDefinition CreateModel()
        {
            return new PowershellNotificationChannelDefinition();
        }

        public override string DisplayName { get; set; } = "PowerShell";
    }
}
