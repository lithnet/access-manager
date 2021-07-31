using System;
using System.Collections.ObjectModel;
using Lithnet.AccessManager.Server.Providers;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.SimpleChildWindow;
using Stylet;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Lithnet.AccessManager.Server.UI
{
    public class AmsDirectoryGroupsViewModel : Screen, IHelpLink
    {
        private readonly IDialogCoordinator dialogCoordinator;
        private readonly INotifyModelChangedEventPublisher eventPublisher;
        private readonly IShellExecuteProvider shellExecuteProvider;
        private readonly IAmsGroupProvider groupProvider;
        private readonly IViewModelFactory<AmsGroupViewModel, IAmsGroup> factory;
        private readonly ILogger<AmsDirectoryGroupsViewModel> logger;

        public AmsDirectoryGroupsViewModel(IDialogCoordinator dialogCoordinator, INotifyModelChangedEventPublisher eventPublisher, IShellExecuteProvider shellExecuteProvider, IAmsGroupProvider groupProvider, IViewModelFactory<AmsGroupViewModel, IAmsGroup> factory, ILogger<AmsDirectoryGroupsViewModel> logger, IViewModelFactory<EnterpriseEditionBannerViewModel, EnterpriseEditionBannerModel> enterpriseEditionViewModelFactory)
        {
            this.dialogCoordinator = dialogCoordinator;
            this.eventPublisher = eventPublisher;
            this.shellExecuteProvider = shellExecuteProvider;
            this.groupProvider = groupProvider;
            this.factory = factory;
            this.logger = logger;

            this.DisplayName = "Groups";
            this.Groups = new BindableCollection<AmsGroupViewModel>();
            this.SelectedItems = new ObservableCollection<AmsGroupViewModel>();
            this.SelectedItems.CollectionChanged += this.SelectedItems_CollectionChanged;
            this.EnterpriseEdition = enterpriseEditionViewModelFactory.CreateViewModel(new EnterpriseEditionBannerModel
            {
                RequiredFeature = Enterprise.LicensedFeatures.AmsRegisteredDeviceSupport,
                Link = Constants.EnterpriseEditionLearnMoreLinkAmsDevices
            });
        }

        private void SelectedItems_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            this.NotifyOfPropertyChange(() => this.CanDelete);
            this.NotifyOfPropertyChange(() => this.CanEdit);
        }

        public EnterpriseEditionBannerViewModel EnterpriseEdition { get; set; }

        protected override void OnInitialActivate()
        {
            Task.Run(async () => await this.Initialize());
        }

        private async Task Initialize()
        {
            try
            {
                await foreach (IAmsGroup m in this.groupProvider.GetGroups())
                {
                    this.Groups.Add(factory.CreateViewModel(m));
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Could not initialize the view model");
                this.ErrorMessageText = ex.ToString();
                this.ErrorMessageHeaderText = "An initialization error occurred";
            }

            this.eventPublisher.Register(this);
        }

        public ObservableCollection<AmsGroupViewModel> SelectedItems { get; }

        public string ErrorMessageText { get; set; }

        public string ErrorMessageHeaderText { get; set; }

        public BindableCollection<AmsGroupViewModel> Groups { get; }

        public AmsGroupViewModel SelectedItem { get; set; }

        public async Task Add()
        {
            try
            {
                DialogWindow w = new DialogWindow
                {
                    Title = "Add group",
                    SaveButtonIsDefault = true
                };

                var m = await this.groupProvider.CreateGroup();

                var vm = this.factory.CreateViewModel(m);
                w.DataContext = vm;

                await this.GetWindow().ShowChildWindowAsync(w);

                if (w.Result == MessageDialogResult.Affirmative)
                {
                    m = await this.groupProvider.UpdateGroup(m);
                    this.Groups.Add(this.factory.CreateViewModel(m));

                    foreach (var d in vm.MembersToAdd)
                    {
                        await this.groupProvider.AddToGroup(m, d.Value);
                    }
                }

            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIGenericError, ex, "Could not complete the operation");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Could not complete the operation\r\n{ex.Message}");
            }
        }

        public bool CanEdit => this.SelectedItems.Count == 1;

        public async Task Edit()
        {
            try
            {
                var selectedKey = this.SelectedItem;

                if (selectedKey == null)
                {
                    return;
                }
                var m = await this.groupProvider.CloneGroup(selectedKey.Model);
                var vm = this.factory.CreateViewModel(m);

                DialogWindow w = new DialogWindow
                {
                    Title = "Edit group",
                    SaveButtonIsDefault = true,
                    DataContext = vm
                };

                await this.GetWindow().ShowChildWindowAsync(w);

                if (w.Result == MessageDialogResult.Affirmative)
                {
                    selectedKey.Name = vm.Name;
                    selectedKey.Description = vm.Description;
                    await this.groupProvider.UpdateGroup(selectedKey.Model);

                    foreach (var d in vm.MembersToAdd)
                    {
                        await this.groupProvider.AddToGroup(selectedKey.Model, d.Value);
                    }

                    foreach (var d in vm.MembersToRemove)
                    {
                        await this.groupProvider.RemoveFromGroup(selectedKey.Model, d.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIGenericError, ex, "Could not complete the operation");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Could not complete the operation\r\n{ex.Message}");
            }
        }

        public bool CanDelete => this.SelectedItems.Count > 0 && this.SelectedItems.All(t => t.Type == AmsGroupType.Normal);

        public async Task Delete()
        {
            try
            {
                var selectedItems = this.SelectedItems.ToList();

                if (selectedItems.Count == 0)
                {
                    return;
                }

                MetroDialogSettings s = new MetroDialogSettings
                {
                    AnimateShow = false,
                    AnimateHide = false
                };

                string message = "this group";

                if (selectedItems.Count > 1)
                {
                    message = $"these {selectedItems.Count} groups";
                }

                if (await this.dialogCoordinator.ShowMessageAsync(this, "Warning", $"Are you sure you want to delete {message}? This operation can not be undone", MessageDialogStyle.AffirmativeAndNegative, s) == MessageDialogResult.Affirmative)
                {
                    foreach (var item in selectedItems.Where(t => t.Type == AmsGroupType.Normal))
                    {
                        await this.groupProvider.DeleteGroup(item.Model);
                        this.Groups.Remove(item);
                        this.SelectedItem = this.Groups.FirstOrDefault();
                    }

                    this.NotifyOfPropertyChange(() => this.CanEdit);
                    this.NotifyOfPropertyChange(() => this.CanDelete);
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIGenericError, ex, "Could not complete the operation");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Could not complete the operation\r\n{ex.Message}");
            }
        }

        public string HelpLink => Constants.HelpLinkPageJitAccess;

        public async Task Help()
        {
            await this.shellExecuteProvider.OpenWithShellExecute(this.HelpLink);
        }
    }
}
