using System;
using System.Linq;
using Lithnet.AccessManager.Api;
using Stylet;
using System.Threading.Tasks;
using System.Windows;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.SimpleChildWindow;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Lithnet.AccessManager.Server.UI
{
    public class PasswordPoliciesViewModel : Screen
    {
        private readonly PasswordPolicyOptions passwordPolicy;
        private readonly IViewModelFactory<PasswordPolicyViewModel, PasswordPolicyEntry> policyFactory;
        private readonly INotifyModelChangedEventPublisher eventPublisher;
        private readonly IDialogCoordinator dialogCoordinator;
        private readonly ILogger<PasswordPoliciesViewModel> logger;

        public PasswordPoliciesViewModel(INotifyModelChangedEventPublisher eventPublisher, PasswordPolicyOptions passwordPolicy, IViewModelFactory<PasswordPolicyViewModel, PasswordPolicyEntry> policyFactory, IDialogCoordinator dialogCoordinator, ILogger<PasswordPoliciesViewModel> logger)
        {
            this.eventPublisher = eventPublisher;
            this.passwordPolicy = passwordPolicy;
            this.policyFactory = policyFactory;
            this.dialogCoordinator = dialogCoordinator;
            this.logger = logger;
            this.Policies = new BindableCollection<PasswordPolicyViewModel>();
            this.SetupPolicies();
        }

        [NotifyModelChangedCollection]
        public BindableCollection<PasswordPolicyViewModel> Policies { get; }

        public PasswordPolicyViewModel SelectedPolicy { get; set; }

        [NotifyModelChangedProperty]
        public PasswordPolicyViewModel DefaultPolicy { get; set; }

        private void SetupPolicies()
        {
            foreach (var m in this.passwordPolicy.Policies)
            {
                this.Policies.Add(this.policyFactory.CreateViewModel(m));
            }

            this.DefaultPolicy = this.policyFactory.CreateViewModel(this.passwordPolicy.DefaultPolicy ?? new PasswordPolicyEntry());
            this.DefaultPolicy.IsDefault = true;

            this.eventPublisher.Register(this);
        }

        public bool CanMoveUp => this.SelectedPolicy != null && this.GetSelectedItemIndex() > 0;

        public async Task MoveUp()
        {
            try
            {
                var selectedItem = this.SelectedPolicy;

                if (selectedItem == null)
                {
                    return;
                }

                int oldIndex = this.Policies.IndexOf(selectedItem);
                int newIndex = Math.Max(oldIndex - 1, 0);

                this.Policies.Move(oldIndex, newIndex);

                var item = this.passwordPolicy.Policies[oldIndex];
                this.passwordPolicy.Policies.RemoveAt(oldIndex);
                this.passwordPolicy.Policies.Insert(newIndex, item);

                this.NotifyOfPropertyChange(() => CanMoveUp);
                this.NotifyOfPropertyChange(() => CanMoveDown);
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIGenericError, ex, "Could not move policy");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Could not move the policy\r\n{ex.Message}");
            }
        }

        public bool CanMoveDown => this.SelectedPolicy != null && this.GetSelectedItemIndex() < (this.Policies.Count - 1);

        public async Task MoveDown()
        {
            try
            {
                var selectedItem = this.SelectedPolicy;

                if (selectedItem == null)
                {
                    return;
                }

                int oldIndex = this.Policies.IndexOf(selectedItem);
                int newIndex = Math.Min(oldIndex + 1, this.Policies.Count - 1);

                this.Policies.Move(oldIndex, newIndex);

                var item = this.passwordPolicy.Policies[oldIndex];
                this.passwordPolicy.Policies.RemoveAt(oldIndex);
                this.passwordPolicy.Policies.Insert(newIndex, item);

                this.NotifyOfPropertyChange(() => CanMoveUp);
                this.NotifyOfPropertyChange(() => CanMoveDown);
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIGenericError, ex, "Could not move policy");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Could not move the policy\r\n{ex.Message}");
            }
        }

        private int GetSelectedItemIndex()
        {
            var selectedItem = this.SelectedPolicy;

            if (selectedItem == null)
            {
                return -1;
            }

            return this.Policies.IndexOf(selectedItem);
        }


        public async Task Add()
        {
            try
            {
                DialogWindow w = new DialogWindow();
                w.Title = "Add policy";
                w.SaveButtonIsDefault = true;
                var m = new PasswordPolicyEntry();
                m.Id = Guid.NewGuid().ToString();
                var vm = this.policyFactory.CreateViewModel(m);
                w.DataContext = vm;

                while (true)
                {
                    await this.GetWindow().ShowChildWindowAsync(w);

                    if (w.Result != MessageDialogResult.Affirmative)
                    {
                        break;
                    }

                    this.passwordPolicy.Policies.Add(m);
                    this.Policies.Add(vm);
                    break;
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIGenericError, ex, "Could not add policy");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Could not add the policy\r\n{ex.Message}");
            }
        }

        public bool CanEdit => this.SelectedPolicy != null;

        public async Task Edit()
        {
            try
            {
                var selectedItem = this.SelectedPolicy;

                if (selectedItem == null)
                {
                    return;
                }

                DialogWindow w = new DialogWindow();
                w.Title = "Edit policy";
                w.SaveButtonIsDefault = true;

                var m = JsonConvert.DeserializeObject<PasswordPolicyEntry>(JsonConvert.SerializeObject(selectedItem.Model));
                var vm = this.policyFactory.CreateViewModel(m);

                w.DataContext = vm;

                while (true)
                {
                    await this.GetWindow().ShowChildWindowAsync(w);

                    if (w.Result != MessageDialogResult.Affirmative)
                    {
                        break;
                    }

                    int existingPosition = this.Policies.IndexOf(selectedItem);
                    int newPosition = Math.Min(existingPosition, this.Policies.Count);

                    this.passwordPolicy.Policies.Remove(selectedItem.Model);

                    this.Policies.Remove(selectedItem);
                    this.passwordPolicy.Policies.Insert(newPosition, m);
                    this.Policies.Insert(newPosition, vm);
                    this.SelectedPolicy = vm;
                    break;
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIGenericError, ex, "Could not edit policy");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Could not edit the policy\r\n{ex.Message}");
            }
        }

        public bool CanDelete => this.SelectedPolicy != null;

        public async Task Delete()
        {
            try
            {
                var selectedItem = this.SelectedPolicy;

                if (selectedItem == null)
                {
                    return;
                }

                MetroDialogSettings s = new MetroDialogSettings
                {
                    AnimateShow = false,
                    AnimateHide = false
                };

                if (await this.dialogCoordinator.ShowMessageAsync(this, "Confirm", "Are you sure you want to delete this policy?", MessageDialogStyle.AffirmativeAndNegative, s) == MessageDialogResult.Affirmative)
                {
                    var deleting = selectedItem;
                    this.passwordPolicy.Policies.Remove(deleting.Model);
                    this.Policies.Remove(deleting);
                    this.SelectedPolicy = this.Policies.FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIGenericError, ex, "Could not delete policy");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Could not delete the policy\r\n{ex.Message}");
            }
        }

        public bool CanTest => this.SelectedPolicy != null;

        public async Task EditDefault()
        {
            try
            {
                DialogWindow w = new DialogWindow();
                w.Title = "Edit default policy";
                w.SaveButtonIsDefault = true;

                var m = JsonConvert.DeserializeObject<PasswordPolicyEntry>(JsonConvert.SerializeObject(this.passwordPolicy.DefaultPolicy));
                var vm = this.policyFactory.CreateViewModel(m);
                vm.IsDefault = true;
                w.DataContext = vm;

                while (true)
                {
                    await this.GetWindow().ShowChildWindowAsync(w);

                    if (w.Result != MessageDialogResult.Affirmative)
                    {
                        break;
                    }

                    this.DefaultPolicy = vm;
                    passwordPolicy.DefaultPolicy = vm.Model;
                    break;
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIGenericError, ex, "Could not edit policy");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Could not edit the policy\r\n{ex.Message}");
            }
        }

        public async Task OnListViewDoubleClick(object sender, RoutedEventArgs e)
        {
            if (!(((FrameworkElement)(e.OriginalSource)).DataContext is PasswordPolicyViewModel))
            {
                return;
            }

            await this.Edit();
        }
    }
}