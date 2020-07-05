using System;
using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Lithnet.AccessManager.Configuration;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.SimpleChildWindow;
using Newtonsoft.Json;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class WebhookNotificationChannelDefinitionsViewModel : NotificationChannelDefinitionsViewModel<WebhookNotificationChannelDefinition, WebhookNotificationChannelDefinitionViewModel>
    {
        public WebhookNotificationChannelDefinitionsViewModel(IList<WebhookNotificationChannelDefinition> model, IDialogCoordinator dialogCoordinator, INotificationSubscriptionProvider subscriptionProvider, IEventAggregator eventAggregator)
            : base(model, dialogCoordinator, subscriptionProvider, eventAggregator)
        {
        }

        public override string DisplayName { get; set; } = "Webhook";

        protected override WebhookNotificationChannelDefinition CreateModel()
        {
            return new WebhookNotificationChannelDefinition();
        }

        protected override WebhookNotificationChannelDefinitionViewModel CreateViewModel(WebhookNotificationChannelDefinition model)
        {
            return new WebhookNotificationChannelDefinitionViewModel(model, this.NotificationSubscriptions);
        }
    }
}