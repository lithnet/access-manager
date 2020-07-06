using Lithnet.AccessManager.Configuration;
using MahApps.Metro.Controls.Dialogs;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class AuthorizationViewModel : PropertyChangedBase, IHaveDisplayName
    {
        private readonly AuthorizationOptions model;

        public AuthorizationViewModel(AuthorizationOptions model, IDialogCoordinator dialogCoordinator, INotificationSubscriptionProvider subscriptionProvider, IEventAggregator eventAggregator)
        {
            this.model = model;
            this.Targets = new SecurityDescriptorTargetsViewModel(model.BuiltInProvider.Targets, dialogCoordinator, subscriptionProvider, eventAggregator);
        }

        public SecurityDescriptorTargetsViewModel Targets { get; }

        public string DisplayName { get; set; } = "Authorization";
    }
}
