using System.Collections.Generic;
using Lithnet.AccessManager.Configuration;
using MahApps.Metro.Controls.Dialogs;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class SmtpNotificationChannelDefinitionsViewModel : NotificationChannelDefinitionsViewModel<SmtpNotificationChannelDefinition, SmtpNotificationChannelDefinitionViewModel>
    {
        public SmtpNotificationChannelDefinitionsViewModel(IList<SmtpNotificationChannelDefinition> model, SmtpNotificationChannelDefinitionViewModelFactory factory, IDialogCoordinator dialogCoordinator, IEventAggregator eventAggregator) :
            base(model, factory, dialogCoordinator, eventAggregator)
        {
        }

        public override string DisplayName { get; set; } = "SMTP";
    }
}