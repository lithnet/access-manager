using System.Windows;
using Lithnet.AccessManager.Configuration;
using MahApps.Metro.Controls.Dialogs;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class AuditingViewModel : PropertyChangedBase, IHaveDisplayName, IViewAware
    {
        private AuditOptions model;

        public BindableCollection<object> ViewModels { get; }

        public AuditingViewModel(AuditOptions model, IDialogCoordinator dialogCoordinator)
        {
            this.model = model;
            this.PowerShell = new PowershellNotificationChannelDefinitionsViewModel(model.NotificationChannels.Powershell, dialogCoordinator);
            this.WebHooks = new WebhookNotificationChannelDefinitionsViewModel(this.model.NotificationChannels.Webhooks, dialogCoordinator);
            this.Smtp = new SmtpNotificationChannelDefinitionsViewModel(this.model.NotificationChannels.Smtp, dialogCoordinator);

            this.ViewModels = new BindableCollection<object>() { this.PowerShell, this.Smtp, this.WebHooks };
        }

        public PowershellNotificationChannelDefinitionsViewModel PowerShell { get; }

        public SmtpNotificationChannelDefinitionsViewModel Smtp { get; }

        public WebhookNotificationChannelDefinitionsViewModel WebHooks { get; }

        public void AttachView(UIElement view)
        {
            this.View = view;
        }

        public string DisplayName { get; set; } = "Auditing";

        public UIElement View { get; set; }
    }
}
