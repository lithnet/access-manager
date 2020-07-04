using System.Windows;
using Lithnet.AccessManager.Configuration;
using MahApps.Metro.Controls.Dialogs;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class AuditingViewModel : Conductor<PropertyChangedBase>.Collection.OneActive
    {
        private AuditOptions model;

        public AuditingViewModel(AuditOptions model, IDialogCoordinator dialogCoordinator)
        {
            this.model = model;
            this.Items.Add(new PowershellNotificationChannelDefinitionsViewModel(model.NotificationChannels.Powershell, dialogCoordinator));
            this.Items.Add(new WebhookNotificationChannelDefinitionsViewModel(this.model.NotificationChannels.Webhooks, dialogCoordinator));
            this.Items.Add(new SmtpNotificationChannelDefinitionsViewModel(this.model.NotificationChannels.Smtp, dialogCoordinator));

            this.DisplayName = "Auditing";
        }
    }
}
