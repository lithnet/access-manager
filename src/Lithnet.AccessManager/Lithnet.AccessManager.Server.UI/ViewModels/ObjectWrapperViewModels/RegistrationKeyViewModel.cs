using Lithnet.AccessManager.Server.Configuration;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;
using Stylet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Lithnet.AccessManager.Server.Providers;

namespace Lithnet.AccessManager.Server.UI
{
    public sealed class RegistrationKeyViewModel : Screen
    {
        private readonly ILogger<RegistrationKeyViewModel> logger;
        private readonly IDialogCoordinator dialogCoordinator;
        private readonly IViewModelFactory<AmsGroupSelectorViewModel> groupSelectorFactory;
        private readonly IRegistrationKeyProvider registrationKeyProvider;

        private HashSet<string> currentMembers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public IRegistrationKey Model { get; }

        public RegistrationKeyViewModel(IRegistrationKey model, ILogger<RegistrationKeyViewModel> logger, IModelValidator<RegistrationKeyViewModel> validator, IDialogCoordinator dialogCoordinator, IViewModelFactory<AmsGroupSelectorViewModel> groupSelectorFactory, IRegistrationKeyProvider registrationKeyProvider)
        {
            this.logger = logger;
            this.dialogCoordinator = dialogCoordinator;
            this.groupSelectorFactory = groupSelectorFactory;
            this.registrationKeyProvider = registrationKeyProvider;
            this.Model = model;
            this.Validator = validator;
            this.Validate();

            this.Groups = new BindableCollection<IAmsGroup>();
            this.SelectedItems = new ObservableCollection<IAmsGroup>();
            this.SelectedItems.CollectionChanged += this.SelectedItems_CollectionChanged;
        }

        public string Key
        {
            get => this.Model.Key;
            set => this.Model.Key = value;
        }

        public int ActivationCount
        {
            get => this.Model.ActivationCount;
            set => this.Model.ActivationCount = value;
        }

        public bool IsActivationLimited
        {
            get => this.ActivationLimit > 0;
            set => this.ActivationLimit = value ? 1 : 0;
        }

        public string ActivationLimitDescription => this.ActivationLimit > 0 ? this.ActivationLimit.ToString() : "No limit";

        public int ActivationLimit
        {
            get => this.Model.ActivationLimit;
            set => this.Model.ActivationLimit = value;
        }

        public bool Enabled
        {
            get => this.Model.Enabled;
            set => this.Model.Enabled = value;
        }

        public string Name
        {
            get => this.Model.Name;
            set => this.Model.Name = value;
        }

        public bool ApprovalRequired
        {
            get => this.Model.ApprovalRequired;
            set => this.Model.ApprovalRequired = value;
        }

        public void ResetActivationCount()
        {
            this.ActivationCount = 0;
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

                await foreach (var m in this.registrationKeyProvider.GetRegistrationKeyGroups(this.Model))
                {
                    this.Groups.Add(m);
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
        public BindableCollection<IAmsGroup> Groups { get; }

        public IAmsGroup SelectedItem { get; set; }

        public ObservableCollection<IAmsGroup> SelectedItems { get; }

        public Dictionary<string, IAmsGroup> MembersToAdd { get; } = new Dictionary<string, IAmsGroup>(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, IAmsGroup> MembersToRemove { get; } = new Dictionary<string, IAmsGroup>(StringComparer.OrdinalIgnoreCase);

        public async Task Add()
        {
            try
            {
                var selectorVm = this.groupSelectorFactory.CreateViewModel();

                ExternalDialogWindow w = new ExternalDialogWindow()
                {
                    Title = "Select group",
                    DataContext = selectorVm,
                    SaveButtonName = "Select...",
                    SaveButtonIsDefault = true,
                    Owner = this.GetWindow()
                };

                if (!w.ShowDialog() ?? false)
                {
                    return;
                }

                if (selectorVm.SelectedItem != null)
                {
                    if (currentMembers.Add(selectorVm.SelectedItem.Sid))
                    {
                        if (this.MembersToAdd.TryAdd(selectorVm.SelectedItem.Sid, selectorVm.SelectedItem.Model))
                        {
                            this.Groups.Add(selectorVm.SelectedItem.Model);
                            this.MembersToRemove.Remove(selectorVm.SelectedItem.Sid);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIGenericError, ex, "Could not add group");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Could not add the group\r\n{ex.Message}");
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

                string message = "this group";

                if (selectedItems.Count > 1)
                {
                    message = $"these {selectedItems.Count} groups";
                }

                if (await this.dialogCoordinator.ShowMessageAsync(this, "Confirm", $"Are you sure you want to remove {message}?", MessageDialogStyle.AffirmativeAndNegative, s) == MessageDialogResult.Affirmative)
                {
                    foreach (var item in selectedItems)
                    {
                        if (currentMembers.Remove(item.Sid))
                        {
                            if (this.MembersToRemove.TryAdd(item.Sid, item))
                            {
                                this.Groups.Remove(item);
                                this.MembersToAdd.Remove(item.Sid);
                            }
                        }
                    }

                    this.NotifyOfPropertyChange(() => this.CanDelete);
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Could not delete the selected groups");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Unable to delete the selected groups\r\n\r\n{ex.Message}");
            }
        }
    }
}
