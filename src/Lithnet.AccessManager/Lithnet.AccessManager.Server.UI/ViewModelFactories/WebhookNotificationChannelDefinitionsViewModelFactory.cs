using System;
using System.Collections.Generic;
using Lithnet.AccessManager.Server.Configuration;
using MahApps.Metro.Controls.Dialogs;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class WebhookNotificationChannelDefinitionsViewModelFactory : NotificationChannelDefinitionsViewModelFactory<WebhookNotificationChannelDefinition, WebhookNotificationChannelDefinitionViewModel>
    {
        private readonly WebhookNotificationChannelDefinitionViewModelFactory factory;
        private readonly IDialogCoordinator dialogCoordinator;
        private readonly Func<IEventAggregator> eventAggregator;
        private readonly Func<INotifyModelChangedEventPublisher> eventPublisher;

        public WebhookNotificationChannelDefinitionsViewModelFactory(WebhookNotificationChannelDefinitionViewModelFactory factory, IDialogCoordinator dialogCoordinator, Func<IEventAggregator> eventAggregator, Func<INotifyModelChangedEventPublisher> eventPublisher) : base(factory)
        {
            this.factory = factory;
            this.dialogCoordinator = dialogCoordinator;
            this.eventAggregator = eventAggregator;
            this.eventPublisher = eventPublisher;
        }

        public override NotificationChannelDefinitionsViewModel<WebhookNotificationChannelDefinition, WebhookNotificationChannelDefinitionViewModel> CreateViewModel(IList<WebhookNotificationChannelDefinition> model)
        {
            return new WebhookNotificationChannelDefinitionsViewModel(model, this.factory, this.dialogCoordinator, this.eventAggregator.Invoke(), this.eventPublisher.Invoke());
        }
    }
}
