using System.Threading.Tasks;
using ControlzEx.Standard;
using Lithnet.AccessManager.Server.Configuration;
using Lithnet.AccessManager.Server.UI.Providers;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.IconPacks;
using Microsoft.Extensions.Logging;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public sealed class AuditingViewModel : Conductor<PropertyChangedBase>.Collection.OneActive, IHelpLink
    {
        private readonly AuditOptions model;
        private readonly INotificationChannelDefinitionsViewModelFactory<PowershellNotificationChannelDefinition, PowershellNotificationChannelDefinitionViewModel> psFactory;
        private readonly INotificationChannelDefinitionsViewModelFactory<SmtpNotificationChannelDefinition, SmtpNotificationChannelDefinitionViewModel> smtpFactory;
        private readonly INotificationChannelDefinitionsViewModelFactory<WebhookNotificationChannelDefinition, WebhookNotificationChannelDefinitionViewModel> whFactory;
        private readonly IViewModelFactory<NotificationChannelSelectionViewModel, AuditNotificationChannels> notificationChannelSelectionViewModelFactory;
        private readonly IShellExecuteProvider shellExecuteProvider;

        public AuditingViewModel(AuditOptions model, IShellExecuteProvider shellExecuteProvider,
            INotificationChannelDefinitionsViewModelFactory<PowershellNotificationChannelDefinition, PowershellNotificationChannelDefinitionViewModel> psFactory,
            INotificationChannelDefinitionsViewModelFactory<SmtpNotificationChannelDefinition, SmtpNotificationChannelDefinitionViewModel> smtpFactory,
            INotificationChannelDefinitionsViewModelFactory<WebhookNotificationChannelDefinition, WebhookNotificationChannelDefinitionViewModel> whFactory,
            IViewModelFactory<NotificationChannelSelectionViewModel, AuditNotificationChannels> notificationChannelSelectionViewModelFactory
            )
        {
            this.model = model;
            this.psFactory = psFactory;
            this.smtpFactory = smtpFactory;
            this.whFactory = whFactory;
            this.shellExecuteProvider = shellExecuteProvider;
            this.notificationChannelSelectionViewModelFactory = notificationChannelSelectionViewModelFactory;
            this.DisplayName = "Auditing";

            this.Powershell = psFactory.CreateViewModel(model.NotificationChannels.Powershell);
            this.Webhook = whFactory.CreateViewModel(model.NotificationChannels.Webhooks);
            this.Smtp = smtpFactory.CreateViewModel(model.NotificationChannels.Smtp);

            this.Items.Add(this.Smtp);
            this.Items.Add(this.Webhook);
            this.Items.Add(this.Powershell);

            this.Notifications = notificationChannelSelectionViewModelFactory.CreateViewModel(model.GlobalNotifications);
        }

        public PackIconUniconsKind Icon => PackIconUniconsKind.FileExclamationAlt;

        public NotificationChannelSelectionViewModel Notifications { get; private set; }

        private NotificationChannelDefinitionsViewModel<PowershellNotificationChannelDefinition, PowershellNotificationChannelDefinitionViewModel> Powershell { get; set; }

        private NotificationChannelDefinitionsViewModel<WebhookNotificationChannelDefinition, WebhookNotificationChannelDefinitionViewModel> Webhook { get; set; }

        private NotificationChannelDefinitionsViewModel<SmtpNotificationChannelDefinition, SmtpNotificationChannelDefinitionViewModel> Smtp { get; set; }
        
        public string HelpLink => Constants.HelpLinkPageAuditing;

        public async Task Help()
        {
            await this.shellExecuteProvider.OpenWithShellExecute(this.HelpLink);
        }
    }
}
