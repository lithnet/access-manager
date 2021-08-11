using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Stylet;
using Lithnet.AccessManager.Server.Providers;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;

namespace Lithnet.AccessManager.Server.UI
{
    public sealed class AmsGroupViewModel : Screen
    {
        private readonly IAmsGroupProvider groupProvider;
        private readonly IDialogCoordinator dialogCoordinator;
        private readonly ILogger<AmsGroupViewModel> logger;
        private readonly IViewModelFactory<AmsDeviceSelectorViewModel> deviceSelectorFactory;
        private HashSet<string> currentMembers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        public IAmsGroup Model { get; }

        public AmsGroupViewModel(IAmsGroup model, IModelValidator<AmsGroupViewModel> validator, IAmsGroupProvider provider, IDialogCoordinator dialogCoordinator, ILogger<AmsGroupViewModel> logger, IViewModelFactory<AmsDeviceSelectorViewModel> deviceSelectorFactory)
        {
            this.Model = model;
            this.groupProvider = provider;
            this.dialogCoordinator = dialogCoordinator;
            this.logger = logger;
            this.deviceSelectorFactory = deviceSelectorFactory;
            this.Validator = validator;

            this.Validate();
            this.Members = new BindableCollection<AmsGroupMemberViewModel>();
            this.SelectedItems = new ObservableCollection<AmsGroupMemberViewModel>();
            this.SelectedItems.CollectionChanged += this.SelectedItems_CollectionChanged;
        }

        private void SelectedItems_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            this.NotifyOfPropertyChange(() => this.CanDelete);
        }

        protected override void OnInitialActivate()
        {
            Task.Run(async () => await this.Initialize());
        }

        private async Task Initialize()
        {
            try
            {
                this.IsLoading = true;

                await foreach (var m in this.groupProvider.GetMemberDevices(this.Model))
                {
                    this.Members.Add(new AmsGroupMemberViewModel(m));
                    currentMembers.Add(m.Sid);
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Could not initialize the view model");
                this.ErrorMessageText = ex.ToString();
                this.ErrorMessageHeaderText = "An initialization error occurred";
            }
            finally
            {
                this.IsLoading = false;
            }
        }

        public string ErrorMessageText { get; set; }

        public string ErrorMessageHeaderText { get; set; }

        public bool IsLoading { get; set; }

        [NotifyModelChangedCollection]
        public BindableCollection<AmsGroupMemberViewModel> Members { get; }

        public AmsGroupMemberViewModel SelectedItem { get; set; }

        public ObservableCollection<AmsGroupMemberViewModel> SelectedItems { get; }

        public string Description
        {
            get => this.Model.Description;
            set => this.Model.Description = value;
        }

        public string Sid
        {
            get => this.Model.Sid;
        }

        public string Name
        {
            get => this.Model.Name;
            set => this.Model.Name = value;
        }

        public AmsGroupType Type
        {
            get => this.Model.Type;
        }

        public Dictionary<string, IDevice> MembersToAdd { get; } = new Dictionary<string, IDevice>(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, IDevice> MembersToRemove { get; } = new Dictionary<string, IDevice>(StringComparer.OrdinalIgnoreCase);

        public async Task Add()
        {
            try
            {
                var selectorVm = this.deviceSelectorFactory.CreateViewModel();
                selectorVm.SelectionMode = System.Windows.Controls.SelectionMode.Extended;

                ExternalDialogWindow w = new ExternalDialogWindow()
                {
                    Title = "Select device",
                    DataContext = selectorVm,
                    SaveButtonName = "Select...",
                    SaveButtonIsDefault = true,
                    Owner = this.GetWindow()
                };

                if (!w.ShowDialog() ?? false)
                {
                    return;
                }

                if (selectorVm.SelectedItems != null)
                {
                    foreach (var item in selectorVm.SelectedItems)
                    {
                        if (currentMembers.Add(item.Sid))
                        {
                            if (this.MembersToAdd.TryAdd(item.Sid, item.Model))
                            {
                                this.Members.Add(new AmsGroupMemberViewModel(item.Model));
                                this.MembersToRemove.Remove(item.Sid);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIGenericError, ex, "Could not add group members");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Could not add the group members\r\n{ex.Message}");
            }
        }

        public bool CanDelete => this.SelectedItems.Any();

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

                string message = "this member";

                if (selectedItems.Count > 1)
                {
                    message = $"these {selectedItems.Count} members";
                }

                if (await this.dialogCoordinator.ShowMessageAsync(this, "Confirm", $"Are you sure you want to remove {message}?", MessageDialogStyle.AffirmativeAndNegative, s) == MessageDialogResult.Affirmative)
                {
                    foreach (var item in selectedItems)
                    {
                        if (item.Model is IDevice d)
                        {
                            if (currentMembers.Remove(d.Sid))
                            {
                                if (this.MembersToRemove.TryAdd(d.Sid, d))
                                {
                                    this.Members.Remove(item);
                                    this.MembersToAdd.Remove(d.Sid);
                                }
                            }
                        }
                    }

                    this.NotifyOfPropertyChange(() => this.CanDelete);
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Could not delete the selected members");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Unable to delete the selected members\r\n\r\n{ex.Message}");
            }
        }
    }
}
