using System;
using System.Linq;
using System.Windows;
using System.Windows.Media.Animation;
using Lithnet.AccessManager.Configuration;
using Lithnet.AccessManager.Server.UI.Interop;
using MahApps.Metro.Controls.Dialogs;
using NLog.Web.LayoutRenderers;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class SecurityDescriptorTargetViewModel : PropertyChangedBase, IHandle<NotificationSubscriptionChangedEvent>, IViewAware
    {
        private readonly INotificationSubscriptionProvider subscriptions;

        private readonly IEventAggregator eventAggregator;

        public SecurityDescriptorTarget Model { get; }

        public SecurityDescriptorTargetViewModel(SecurityDescriptorTarget model, IDialogCoordinator dialogCoordinator, INotificationSubscriptionProvider subscriptionProvider, IEventAggregator eventAggregator)
        {
            this.Model = model;
            this.subscriptions = subscriptionProvider;
            this.eventAggregator = eventAggregator;

            this.SuccessSubscriptions = subscriptionProvider.GetSubscriptions(this.Model.Notifications.OnSuccess);
            this.FailureSubscriptions = subscriptionProvider.GetSubscriptions(this.Model.Notifications.OnFailure);

            this.AvailableSuccessSubscriptions = new BindableCollection<SubscriptionViewModel>(subscriptionProvider.Subscriptions.Except(this.SuccessSubscriptions));
            this.AvailableFailureSubscriptions = new BindableCollection<SubscriptionViewModel>(subscriptionProvider.Subscriptions.Except(this.FailureSubscriptions));

            this.eventAggregator.Subscribe(this);
        }

        public BindableCollection<SubscriptionViewModel> SuccessSubscriptions { get; set; }

        public BindableCollection<SubscriptionViewModel> FailureSubscriptions { get; set; }

        public BindableCollection<SubscriptionViewModel> AvailableSuccessSubscriptions { get; }

        public BindableCollection<SubscriptionViewModel> AvailableFailureSubscriptions { get; }

        public AuthorizationMode AuthorizationMode { get => this.Model.AuthorizationMode; set => this.Model.AuthorizationMode = value; }

        public bool IsModePermission { get => this.AuthorizationMode == AuthorizationMode.SecurityDescriptor; set => this.AuthorizationMode = value ? AuthorizationMode.SecurityDescriptor : AuthorizationMode.PowershellScript; }

        public bool IsModeScript { get => this.AuthorizationMode == AuthorizationMode.PowershellScript; set => this.AuthorizationMode = value ? AuthorizationMode.PowershellScript : AuthorizationMode.SecurityDescriptor; }


        public string Id { get => this.Model.Id; set => this.Model.Id = value; }

        public string Script { get => this.Model.Script; set => this.Model.Script = value; }

        public TargetType Type { get => this.Model.Type; set => this.Model.Type = value; }

        public string SecurityDescriptor { get => this.Model.SecurityDescriptor; set => this.Model.SecurityDescriptor = value; }

        public string JitAuthorizingGroup { get => this.Model.Jit.AuthorizingGroup; set => this.Model.Jit.AuthorizingGroup = value; }

        public string JitGroupName => this.JitAuthorizingGroup;

        public TimeSpan JitExpireAfter { get => this.Model.Jit.ExpireAfter; set => this.Model.Jit.ExpireAfter = value; }

        public TimeSpan LapsExpireAfter { get => this.Model.Laps.ExpireAfter; set => this.Model.Laps.ExpireAfter = value; }

        public int LapsExpireMinutes { get => (int)this.LapsExpireAfter.TotalMinutes; set => this.LapsExpireAfter = new TimeSpan(0, value, 0); }

        public bool ExpireLapsPassword
        {
            get => this.LapsExpireAfter.TotalSeconds > 0; set
            {
                if (value)
                {
                    if (this.LapsExpireAfter.TotalSeconds <= 0)
                    {
                        this.LapsExpireAfter = new TimeSpan(0, 15, 0);
                    }
                }
                else
                {
                    this.LapsExpireAfter = new TimeSpan(0);
                }
            }
        }

        public int JitExpireMinutes { get => (int)this.JitExpireAfter.TotalMinutes; set => this.JitExpireAfter = new TimeSpan(0, value, 0); }


        public PasswordStorageLocation RetrievalLocation { get => this.Model.Laps.RetrievalLocation; set => this.Model.Laps.RetrievalLocation = value; }

        public string DisplayName => this.Id;

        public SubscriptionViewModel SelectedSuccessSubscription { get; set; }

        public SubscriptionViewModel SelectedFailureSubscription { get; set; }

        public SubscriptionViewModel SelectedAvailableSuccessSubscription { get; set; }

        public SubscriptionViewModel SelectedAvailableFailureSubscription { get; set; }

        public void EditPermissions()
        {

        }

        public void SelectScript()
        {

        }

        public void SelectJitGroup()
        {
            
        }

        public void SelectTarget()
        {
            NativeMethods.ShowContainerDialog(this.GetHandle(), "Select domain", "Select domain");
        }

        public void AddSuccess()
        {
            this.Model.Notifications.OnSuccess.Add(this.SelectedAvailableSuccessSubscription.Id);
            this.SuccessSubscriptions.Add(this.SelectedAvailableSuccessSubscription);
            this.AvailableSuccessSubscriptions.Remove(this.SelectedAvailableSuccessSubscription);
        }

        public bool CanAddSuccess => this.SelectedAvailableSuccessSubscription != null;

        public void RemoveSuccess()
        {
            string matchingItem = this.Model.Notifications.OnSuccess.FirstOrDefault(t => string.Equals(t, this.SelectedSuccessSubscription.Id, System.StringComparison.OrdinalIgnoreCase));
            this.Model.Notifications.OnSuccess.Remove(matchingItem);
            this.AvailableSuccessSubscriptions.Add(this.SelectedSuccessSubscription);
            this.SuccessSubscriptions.Remove(this.SelectedSuccessSubscription);
        }

        public bool CanRemoveSuccess => this.SelectedSuccessSubscription != null;

        public void AddFailure()
        {
            this.Model.Notifications.OnFailure.Add(this.SelectedAvailableFailureSubscription.Id);
            this.FailureSubscriptions.Add(this.SelectedAvailableFailureSubscription);
            this.AvailableFailureSubscriptions.Remove(this.SelectedAvailableFailureSubscription);
        }

        public bool CanAddFailure => this.SelectedAvailableFailureSubscription != null;

        public void RemoveFailure()
        {
            string matchingItem = this.Model.Notifications.OnFailure.FirstOrDefault(t => string.Equals(t, this.SelectedFailureSubscription.Id, System.StringComparison.OrdinalIgnoreCase));
            this.Model.Notifications.OnFailure.Remove(matchingItem);
            this.AvailableFailureSubscriptions.Add(this.SelectedFailureSubscription);
            this.FailureSubscriptions.Remove(this.SelectedFailureSubscription);
        }

        public bool CanRemoveFailure => this.SelectedFailureSubscription != null;

        public UIElement View { get; set; }

        public void Handle(NotificationSubscriptionChangedEvent message)
        {
            var sub = new SubscriptionViewModel(message.ModifiedObject);

            if (message.ModificationType == ModificationType.Added)
            {
                this.AvailableSuccessSubscriptions.Add(sub);
                this.AvailableFailureSubscriptions.Add(sub);
            }
            else if (message.ModificationType == ModificationType.Deleted)
            {
                this.AvailableSuccessSubscriptions.Remove(sub);
                this.SuccessSubscriptions.Remove(sub);

                this.AvailableFailureSubscriptions.Remove(sub);
                this.FailureSubscriptions.Remove(sub);
            }
            else if (message.ModificationType == ModificationType.Modified)
            {
                foreach (var item in this.SuccessSubscriptions)
                {
                    if (item == sub)
                    {
                        item.DisplayName = sub.DisplayName;
                    }
                }

                foreach (var item in this.AvailableSuccessSubscriptions)
                {
                    if (item == sub)
                    {
                        item.DisplayName = sub.DisplayName;
                    }
                }

                foreach (var item in this.FailureSubscriptions)
                {
                    if (item == sub)
                    {
                        item.DisplayName = sub.DisplayName;
                    }
                }

                foreach (var item in this.AvailableFailureSubscriptions)
                {
                    if (item == sub)
                    {
                        item.DisplayName = sub.DisplayName;
                    }
                }
            }
        }

        public void AttachView(UIElement view)
        {
            this.View = view;
        }
    }
}