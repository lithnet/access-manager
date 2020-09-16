using System;
using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows;
using Lithnet.AccessManager.Server.Configuration;
using Lithnet.AccessManager.Server.UI.Interop;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.SimpleChildWindow;
using Newtonsoft.Json;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class SecurityDescriptorTargetsViewModel : PropertyChangedBase, IHaveDisplayName, IViewAware
    {
        private readonly SecurityDescriptorTargetViewModelFactory factory;

        private readonly IDiscoveryServices discoveryServices;

        protected IList<SecurityDescriptorTarget> Model { get; }

        protected IDialogCoordinator DialogCoordinator { get; }

        [NotifyModelChangedCollection]
        public BindableCollection<SecurityDescriptorTargetViewModel> ViewModels { get; }

        public SecurityDescriptorTargetsViewModel(IList<SecurityDescriptorTarget> model, SecurityDescriptorTargetViewModelFactory factory, IDialogCoordinator dialogCoordinator, INotifyModelChangedEventPublisher eventPublisher, IDiscoveryServices discoveryServices)
        {
            this.factory = factory;
            this.Model = model;
            this.discoveryServices = discoveryServices;
            this.DialogCoordinator = dialogCoordinator;
            this.ViewModels = new BindableCollection<SecurityDescriptorTargetViewModel>(this.Model.Select(factory.CreateViewModel));
            eventPublisher.Register(this);
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
            await vm.Initialize();

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
            w.SaveButtonIsDefault = true;

            var m = JsonConvert.DeserializeObject<SecurityDescriptorTarget>(JsonConvert.SerializeObject(this.SelectedItem.Model));
            var vm = this.factory.CreateViewModel(m);
            await vm.Initialize();

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

        public async Task ImportRules()
        {
            string container = this.ShowContainerDialog();

            if (container == null)
            {
                return;
            }


        }

        private string ShowContainerDialog()
        {
            string path = Domain.GetComputerDomain().GetDirectoryEntry().GetPropertyString("distinguishedName");
            string basePath = this.discoveryServices.GetFullyQualifiedDomainControllerAdsPath(path);
            string initialPath = this.discoveryServices.GetFullyQualifiedAdsPath(path);

            return NativeMethods.ShowContainerDialog(this.GetHandle(), "Select container", "Select container", basePath, initialPath);
        }

        public bool CanEdit => this.SelectedItem != null;

        public async Task Delete()
        {
            var deleting = this.SelectedItem;

            if (deleting == null)
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
