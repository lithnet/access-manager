using ControlzEx.Standard;
using Lithnet.AccessManager.Server.Configuration;
using MahApps.Metro.IconPacks;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public sealed class AuditingViewModel : Conductor<PropertyChangedBase>.Collection.OneActive
    {
        private readonly AuditOptions model;
        private readonly INotificationChannelDefinitionsViewModelFactory<PowershellNotificationChannelDefinition, PowershellNotificationChannelDefinitionViewModel> psFactory;
        private readonly INotificationChannelDefinitionsViewModelFactory<SmtpNotificationChannelDefinition, SmtpNotificationChannelDefinitionViewModel> smtpFactory;
        private readonly INotificationChannelDefinitionsViewModelFactory<WebhookNotificationChannelDefinition, WebhookNotificationChannelDefinitionViewModel> whFactory;
        private readonly INotificationChannelSelectionViewModelFactory notificationChannelSelectionViewModelFactory;

        public AuditingViewModel(AuditOptions model,
            INotificationChannelDefinitionsViewModelFactory<PowershellNotificationChannelDefinition, PowershellNotificationChannelDefinitionViewModel> psFactory,
            INotificationChannelDefinitionsViewModelFactory<SmtpNotificationChannelDefinition, SmtpNotificationChannelDefinitionViewModel> smtpFactory,
            INotificationChannelDefinitionsViewModelFactory<WebhookNotificationChannelDefinition, WebhookNotificationChannelDefinitionViewModel> whFactory,
            INotificationChannelSelectionViewModelFactory notificationChannelSelectionViewModelFactory
            )
        {
            this.model = model;
            this.psFactory = psFactory;
            this.smtpFactory = smtpFactory;
            this.whFactory = whFactory;
            this.notificationChannelSelectionViewModelFactory = notificationChannelSelectionViewModelFactory;
            this.DisplayName = "Auditing";

        }

        protected override void OnInitialActivate()
        {
            this.Powershell = psFactory.CreateViewModel(model.NotificationChannels.Powershell);
            this.Webhook = whFactory.CreateViewModel(model.NotificationChannels.Webhooks);
            this.Smtp = smtpFactory.CreateViewModel(model.NotificationChannels.Smtp);

            this.Items.Add(this.Smtp);
            this.Items.Add(this.Webhook);
            this.Items.Add(this.Powershell);

            this.ActivateItem(this.Smtp);

            this.Notifications = notificationChannelSelectionViewModelFactory.CreateViewModel(model.GlobalNotifications);
        }

        public PackIconUniconsKind Icon => PackIconUniconsKind.FileExclamationAlt;

        public NotificationChannelSelectionViewModel Notifications { get; private set; }

        private NotificationChannelDefinitionsViewModel<PowershellNotificationChannelDefinition, PowershellNotificationChannelDefinitionViewModel> Powershell { get; set; }

        private NotificationChannelDefinitionsViewModel<WebhookNotificationChannelDefinition, WebhookNotificationChannelDefinitionViewModel> Webhook { get; set; }

        private NotificationChannelDefinitionsViewModel<SmtpNotificationChannelDefinition, SmtpNotificationChannelDefinitionViewModel> Smtp { get; set; }
    }
}
