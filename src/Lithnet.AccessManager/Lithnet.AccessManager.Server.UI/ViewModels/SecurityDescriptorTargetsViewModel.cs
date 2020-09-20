using System;
using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Lithnet.AccessManager.Server.Configuration;
using Lithnet.AccessManager.Server.UI.Interop;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.SimpleChildWindow;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class SecurityDescriptorTargetsViewModel : PropertyChangedBase, IHaveDisplayName, IViewAware
    {
        private readonly SecurityDescriptorTargetViewModelFactory factory;
        private readonly IDiscoveryServices discoveryServices;
        private readonly IDialogCoordinator dialogCoordinator;
        private readonly ILogger<SecurityDescriptorTargetsViewModel> logger;


        public IList<SecurityDescriptorTarget> Model { get; }

        [NotifyModelChangedCollection]
        public BindableCollection<SecurityDescriptorTargetViewModel> ViewModels { get; }

        public SecurityDescriptorTargetsViewModel(IList<SecurityDescriptorTarget> model, SecurityDescriptorTargetViewModelFactory factory, IDialogCoordinator dialogCoordinator, INotifyModelChangedEventPublisher eventPublisher, IDiscoveryServices discoveryServices, ILogger<SecurityDescriptorTargetsViewModel> logger)
        {
            this.factory = factory;
            this.Model = model;
            this.discoveryServices = discoveryServices;
            this.dialogCoordinator = dialogCoordinator;
            this.logger = logger;
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
            var selectedItem = this.SelectedItem;

            if (selectedItem == null)
            {
                return;
            }

            DialogWindow w = new DialogWindow
            {
                Title = "Edit target",
                SaveButtonIsDefault = true
            };

            var m = JsonConvert.DeserializeObject<SecurityDescriptorTarget>(JsonConvert.SerializeObject(selectedItem.Model));
            var vm = this.factory.CreateViewModel(m);
            await vm.Initialize();

            w.DataContext = vm;
            vm.IsScriptVisible = selectedItem.IsScriptVisible;

            await this.GetWindow().ShowChildWindowAsync(w);

            if (w.Result == MessageDialogResult.Affirmative)
            {
                this.Model.Remove(selectedItem.Model);

                int existingPosition = this.ViewModels.IndexOf(selectedItem);

                this.ViewModels.Remove(selectedItem);
                this.Model.Add(m);
                this.ViewModels.Insert(Math.Min(existingPosition, this.ViewModels.Count), vm);
                this.SelectedItem = vm;
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

        public async Task Delete(System.Collections.IList items)
        {
            if (items == null)
            {
                return;
            }

            var itemsToDelete = items.Cast<SecurityDescriptorTargetViewModel>().ToList();

            MetroDialogSettings s = new MetroDialogSettings
            {
                AnimateShow = false,
                AnimateHide = false
            };

            string message = itemsToDelete.Count == 1 ? "Are you sure you want to delete this rule?" : $"Are you sure you want to delete {itemsToDelete.Count} rules?";

            if (await this.dialogCoordinator.ShowMessageAsync(this, "Confirm", message, MessageDialogStyle.AffirmativeAndNegative, s) == MessageDialogResult.Affirmative)
            {
                foreach (var deleting in itemsToDelete)
                {
                    this.Model.Remove(deleting.Model);
                    this.ViewModels.Remove(deleting);
                }

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
