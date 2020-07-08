using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Lithnet.AccessManager.Configuration;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.SimpleChildWindow;
using Newtonsoft.Json;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public abstract class NotificationChannelDefinitionsViewModel<TModel, TViewModel> : PropertyChangedBase, IHaveDisplayName, IViewAware where TViewModel : NotificationChannelDefinitionViewModel<TModel> where TModel : NotificationChannelDefinition
    {
        protected IList<TModel> Model { get; }

        protected IDialogCoordinator DialogCoordinator { get; }

        protected IEventAggregator EventAggregator { get; }

        private readonly NotificationChannelDefinitionViewModelFactory<TModel, TViewModel> factory;

        public BindableCollection<TViewModel> ViewModels { get; }

        protected NotificationChannelDefinitionsViewModel(IList<TModel> model, NotificationChannelDefinitionViewModelFactory<TModel, TViewModel> factory, IDialogCoordinator dialogCoordinator, IEventAggregator eventAggregator)
        {
            this.factory = factory;
            this.Model = model;
            this.EventAggregator = eventAggregator;
            this.DialogCoordinator = dialogCoordinator;
            this.ViewModels = new BindableCollection<TViewModel>(this.Model.Select(t => this.factory.CreateViewModel(t)));
        }

        public TViewModel SelectedItem { get; set; }

        public async Task Add()
        {
            DialogWindow w = new DialogWindow();
            w.Title = "Add notification channel";
            w.SaveButtonIsDefault = true;
            var m = this.factory.CreateModel();
            var vm = this.factory.CreateViewModel(m);
            w.DataContext = vm;
            vm.Enabled = true;
            vm.Id = Guid.NewGuid().ToString();

            await this.GetWindow().ShowChildWindowAsync(w);

            if (w.Result == MessageDialogResult.Affirmative)
            {
                this.Model.Add(m);
                this.ViewModels.Add(vm);
                this.EventAggregator.Publish(new NotificationSubscriptionChangedEvent { ModificationType = ModificationType.Added, ModifiedObject = m });
            }
        }
        public async Task Edit()
        {
            DialogWindow w = new DialogWindow();
            w.Title = "Edit notification channel";
            w.SaveButtonIsDefault = true;

            var m = JsonConvert.DeserializeObject<TModel>(JsonConvert.SerializeObject(this.SelectedItem.Model));
            var vm = this.factory.CreateViewModel(m);

            w.DataContext = vm;

            await this.GetWindow().ShowChildWindowAsync(w);

            if (w.Result == MessageDialogResult.Affirmative)
            {
                this.Model.Remove(this.SelectedItem.Model);

                int existingPosition = this.ViewModels.IndexOf(this.SelectedItem);

                this.ViewModels.Remove(this.SelectedItem);
                this.Model.Add(m);
                this.ViewModels.Insert(Math.Min(existingPosition, this.ViewModels.Count), vm);
                this.SelectedItem = vm;
                this.EventAggregator.Publish(new NotificationSubscriptionChangedEvent { ModificationType = ModificationType.Modified, ModifiedObject = m });
            }
        }

        public bool CanEdit => this.SelectedItem != null;

        public async Task Delete()
        {
            if (this.SelectedItem == null)
            {
                return;
            }

            MetroDialogSettings s = new MetroDialogSettings
            {
                AnimateShow = false,
                AnimateHide = false
            };

            if (await this.DialogCoordinator.ShowMessageAsync(this, "Confirm", "Are you sure you want to delete this channel?", MessageDialogStyle.AffirmativeAndNegative, s) == MessageDialogResult.Affirmative)
            {
                var deleting = this.SelectedItem;
                this.Model.Remove(deleting.Model);
                this.ViewModels.Remove(deleting);
                this.SelectedItem = this.ViewModels.FirstOrDefault();
                this.EventAggregator.Publish(new NotificationSubscriptionChangedEvent { ModificationType = ModificationType.Deleted, ModifiedObject = deleting.Model });
            }
        }

        public bool CanDelete => this.SelectedItem != null;

        public void AttachView(UIElement view)
        {
            this.View = view;
        }

        public abstract string DisplayName { get; set; }

        public UIElement View { get; set; }
    }
}
