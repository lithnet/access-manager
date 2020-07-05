using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Lithnet.AccessManager.Configuration;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.SimpleChildWindow;
using Newtonsoft.Json;
using NLog.Targets.Wrappers;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class SmtpNotificationChannelDefinitionsViewModel : NotificationChannelDefinitionsViewModel<SmtpNotificationChannelDefinition, SmtpNotificationChannelDefinitionViewModel>
    {
        public SmtpNotificationChannelDefinitionsViewModel(IList<SmtpNotificationChannelDefinition> model, IDialogCoordinator dialogCoordinator, INotificationSubscriptionProvider subscriptionProvider, IEventAggregator eventAggregator)
            : base(model, dialogCoordinator, subscriptionProvider, eventAggregator)
        {
        }

        public override string DisplayName { get; set; } = "SMTP";

        protected override SmtpNotificationChannelDefinitionViewModel CreateViewModel(SmtpNotificationChannelDefinition model)
        {
            return new SmtpNotificationChannelDefinitionViewModel(model, this.NotificationSubscriptions);
        }

        protected override SmtpNotificationChannelDefinition CreateModel()
        {
            return new SmtpNotificationChannelDefinition();
        }
    }
}