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
    public class SecurityDescriptorTargetsViewModel : PropertyChangedBase, IHaveDisplayName, IViewAware
    {
        private readonly SecurityDescriptorTargetViewModelFactory factory;

        protected IList<SecurityDescriptorTarget> Model { get; }

        protected IDialogCoordinator DialogCoordinator { get; }

        public BindableCollection<SecurityDescriptorTargetViewModel> ViewModels { get; }

        public SecurityDescriptorTargetsViewModel(IList<SecurityDescriptorTarget> model, SecurityDescriptorTargetViewModelFactory factory, IDialogCoordinator dialogCoordinator)
        {
            this.factory = factory;
            this.Model = model;
            this.DialogCoordinator = dialogCoordinator;
            this.ViewModels = new BindableCollection<SecurityDescriptorTargetViewModel>(this.Model.Select(factory.CreateViewModel));
        }

        protected SecurityDescriptorTarget CreateModel()
        {
            return new SecurityDescriptorTarget();
        }

        public SecurityDescriptorTargetViewModel SelectedItem { get; set; }

        public async Task Add()
        {
            DialogWindow w = new DialogWindow();
            w.Title = "Add target";
            var m = this.CreateModel();
            var vm = this.factory.CreateViewModel(m);
            w.DataContext = vm;
            w.SaveButtonIsDefault = true;

            await this.GetWindow().ShowChildWindowAsync(w);

            if (w.Result == MessageDialogResult.Affirmative)
            {
                this.Model.Add(m);
                this.ViewModels.Add(vm);
            }
        }
        public async Task Edit()
        {
            DialogWindow w = new DialogWindow();
            w.Title = "Edit target";
            w.HorizontalContentAlignment = HorizontalAlignment.Stretch; 
            w.VerticalContentAlignment = VerticalAlignment.Stretch;
            w.SaveButtonIsDefault = true;

            var m = JsonConvert.DeserializeObject<SecurityDescriptorTarget>(JsonConvert.SerializeObject(this.SelectedItem.Model));
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

            if (await this.DialogCoordinator.ShowMessageAsync(this, "Confirm", "Are you sure you want to delete this target?", MessageDialogStyle.AffirmativeAndNegative, s) == MessageDialogResult.Affirmative)
            {
                var deleting = this.SelectedItem;
                this.Model.Remove(deleting.Model);
                this.ViewModels.Remove(deleting);
                this.SelectedItem = this.ViewModels.FirstOrDefault();
            }
        }

        public bool CanDelete => this.SelectedItem != null;

        public void AttachView(UIElement view)
        {
            this.View = view;
        }

        public string DisplayName { get; set; }

        public UIElement View { get; set; }
    }
}
