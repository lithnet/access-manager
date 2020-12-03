using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using CsvHelper;
using Lithnet.AccessManager.Server.Authorization;
using Lithnet.AccessManager.Server.Configuration;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Newtonsoft.Json;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class SecurityDescriptorTargetsViewModel : Screen
    {
        private const int childWindowHeight = 586;
        private const int childWindowWidth = 862;
        private readonly IComputerTargetProvider computerTargetProvider;
        private readonly SecurityDescriptorTargetViewModelComparer customComparer;
        private readonly IDialogCoordinator dialogCoordinator;
        private readonly IDirectory directory;
        private readonly ISecurityDescriptorTargetViewModelFactory factory;
        private readonly ILogger<SecurityDescriptorTargetsViewModel> logger;
        private readonly IEffectiveAccessViewModelFactory effectiveAccessFactory;
        private readonly INotifyModelChangedEventPublisher eventPublisher;
        private readonly IShellExecuteProvider shellExecuteProvider;

        private bool firstSearch;
        private ListSortDirection currentSortDirection = ListSortDirection.Ascending;
        private GridViewColumnHeader lastHeaderClicked;
        private HashSet<string> matchedComputerViewModels;
        private IList selectedItems;

        public Task Initialization { get; private set; }

        public SecurityDescriptorTargetsViewModel(IList<SecurityDescriptorTarget> model, ISecurityDescriptorTargetViewModelFactory factory, IDialogCoordinator dialogCoordinator, INotifyModelChangedEventPublisher eventPublisher, ILogger<SecurityDescriptorTargetsViewModel> logger, IDirectory directory, IComputerTargetProvider computerTargetProvider, IEffectiveAccessViewModelFactory effectiveAccessFactory, IShellExecuteProvider shellExecuteProvider)
        {
            this.factory = factory;
            this.Model = model;
            this.dialogCoordinator = dialogCoordinator;
            this.logger = logger;
            this.directory = directory;
            this.computerTargetProvider = computerTargetProvider;
            this.effectiveAccessFactory = effectiveAccessFactory;
            this.shellExecuteProvider = shellExecuteProvider;
            this.customComparer = new SecurityDescriptorTargetViewModelComparer();
            this.ChildDisplaySettings = new SecurityDescriptorTargetViewModelDisplaySettings();
            this.eventPublisher = eventPublisher;

            this.Initialization = this.Initialize();
        }

        public void SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.selectedItems ??= (e.Source as ListView)?.SelectedItems;
            this.NotifyOfPropertyChange(nameof(CanEdit));
        }

        public bool CanDelete => this.SelectedItem != null;

        public bool CanEdit => this.SelectedItem != null && this.selectedItems?.Count == 1;

        public SecurityDescriptorTargetViewModelDisplaySettings ChildDisplaySettings { get; }

        public bool HasLoaded => !this.IsLoading;

        public bool IsLoading { get; set; }

        public ListCollectionView Items { get; private set; }

        public IList<SecurityDescriptorTarget> Model { get; }

        public string SearchText { get; set; }

        public bool IsFilterApplied { get; set; }

        public SecurityDescriptorTargetViewModel SelectedItem { get; set; }

        [NotifyModelChangedCollection]
        public BindableCollection<SecurityDescriptorTargetViewModel> ViewModels { get; private set; }

        public async Task Add()
        {
            ExternalDialogWindow w = new ExternalDialogWindow
            {
                Title = "Add authorization rule",
                SaveButtonIsDefault = true,
                Height = childWindowHeight,
                Width = childWindowWidth,
            };

            var m = new SecurityDescriptorTarget();
            var vm = await this.factory.CreateViewModelAsync(m, this.ChildDisplaySettings);
            w.DataContext = vm;

            if (w.ShowDialog() == true)
            {
                m.CreatedBy = WindowsIdentity.GetCurrent().User.ToString();
                m.Created = DateTime.UtcNow;
                m.LastModifiedBy = WindowsIdentity.GetCurrent().User.ToString();
                m.LastModified = m.Created;

                this.Model.Add(m);
                this.ViewModels.Add(vm);
            }
        }

        public bool CanApplySearchFilter => !string.IsNullOrWhiteSpace(this.SearchText);

        public async Task ApplySearchFilter()
        {
            ProgressDialogController controller = null;

            if (string.IsNullOrWhiteSpace(this.SearchText))
            {
                return;
            }

            try
            {
                controller = await this.dialogCoordinator.ShowProgressAsync(this, "Searching", "Searching...", false);
                controller.SetProgress(0);
                controller.SetIndeterminate();

                await Task.Run(async () =>
                {
                    try
                    {
                        IComputer computer = null;

                        try
                        {
                            this.directory.TryGetComputer(this.SearchText, out computer);
                        }
                        catch (Exception ex)
                        {
                            this.logger.LogTrace(ex, "Could not find computer object for {computer}", this.SearchText);
                        }

                        if (!this.SearchText.StartsWith('?') && computer != null)
                        {
                            if (!firstSearch)
                            {
                                firstSearch = true;
                                controller.SetMessage($"Please wait while we build the cache and search for rules that apply to computer {computer.MsDsPrincipalName}. This may take a minute.");
                            }
                            else
                            {
                                controller.SetMessage($"Searching for rules that apply to computer {computer.MsDsPrincipalName}...");
                            }

                            this.matchedComputerViewModels = new HashSet<string>();

                            foreach (var item in this.computerTargetProvider.GetMatchingTargetsForComputer(computer, this.Model))
                            {
                                this.matchedComputerViewModels.Add(item.Id);
                            }
                        }
                        else
                        {
                            controller.SetMessage($"Searching for rules containing text '{this.SearchText}'...");
                            this.matchedComputerViewModels = null;
                        }
                    }
                    catch (Exception ex)
                    {
                        this.logger.LogError(EventIDs.UIGenericError, ex, "Could not complete the search");
                        await this.dialogCoordinator.ShowMessageAsync(this, "Search error", $"Could not complete the search\r\n{ex.Message}");
                        this.ClearSearchFilter();
                        return;
                    }
                });

                this.Items.Refresh();
                this.IsFilterApplied = true;
            }
            finally
            {
                if (controller != null)
                {
                    if (controller.IsOpen)
                    {
                        await controller.CloseAsync();
                    }
                }
            }
        }

        public bool CanClearSearchFilter => this.IsFilterApplied;

        public void ClearSearchFilter()
        {
            this.SearchText = null;
            this.matchedComputerViewModels = null;
            this.IsFilterApplied = false;
            this.Items.Refresh();
        }

        public void ShowEffectivePermissions()
        {
            var vm = this.effectiveAccessFactory.CreateViewModel(this);

            ExternalDialogWindow window = new ExternalDialogWindow(shellExecuteProvider)
            {
                Title = "Effective Access",
                DataContext = vm,
                CancelButtonName = "Close",
                SaveButtonVisible = false,
                Height = 770
            };

            if (window.ShowDialog() == false)
            {
                return;
            }
        }

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

        public async Task Edit()
        {
            await this.EditItem(this.SelectedItem, this.GetWindow());
        }

        public async Task EditItem(SecurityDescriptorTargetViewModel selectedItem, Window owner)
        {
            try
            {
                if (selectedItem == null)
                {
                    return;
                }

                ExternalDialogWindow w = new ExternalDialogWindow
                {
                    Title = "Edit rule",
                    SaveButtonIsDefault = true,
                    Height = childWindowHeight,
                    Width = childWindowWidth,
                    Owner = owner
                };

                var m = JsonConvert.DeserializeObject<SecurityDescriptorTarget>(JsonConvert.SerializeObject(selectedItem.Model));
                var vm = await this.factory.CreateViewModelAsync(m, this.ChildDisplaySettings);

                vm.IsEditing = true;

                w.DataContext = vm;

                if (w.ShowDialog() == true)
                {
                    this.Model.Remove(selectedItem.Model);

                    int existingPosition = this.ViewModels.IndexOf(selectedItem);

                    this.ViewModels.Remove(selectedItem);
                    this.Model.Add(m);

                    m.LastModifiedBy = WindowsIdentity.GetCurrent().User.ToString();
                    m.LastModified = DateTime.UtcNow;

                    this.ViewModels.Insert(Math.Min(Math.Max(existingPosition, 0), this.ViewModels.Count), vm);
                    this.SelectedItem = vm;
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIGenericError, ex, "Error editing item");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Could not edit the selected item. {ex.Message}");
            }
        }

        public async Task SearchTextBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && this.CanApplySearchFilter)
            {
                e.Handled = true;
                await this.ApplySearchFilter();
            }
        }

        private async Task Initialize()
        {
            await Task.Run(async () =>
             {
                 this.IsLoading = true;
                 this.ViewModels = new BindableCollection<SecurityDescriptorTargetViewModel>();

                 var items = (await Task.WhenAll(this.Model.Select(t => factory.CreateViewModelAsync(t, this.ChildDisplaySettings)))).ToList();

                 this.ViewModels.AddRange(items);

                 Execute.OnUIThreadSync(() =>
                 {
                     this.Items = (ListCollectionView)CollectionViewSource.GetDefaultView(this.ViewModels);
                     this.Items.Filter = this.IsFiltered;
                     this.Items.CustomSort = this.customComparer;
                     this.customComparer.SortDirection = currentSortDirection;
                     this.Items.Refresh();
                 });

                 this.IsLoading = false;

                 eventPublisher.Register(this);
             });
        }

        private bool IsFiltered(object item)
        {
            var trimmedText = this.SearchText?.TrimStart('?');

            if (string.IsNullOrWhiteSpace(trimmedText))
            {
                return true;
            }

            SecurityDescriptorTargetViewModel vm = (SecurityDescriptorTargetViewModel)item;

            if (this.matchedComputerViewModels != null)
            {
                return this.matchedComputerViewModels.Contains(vm.Id);
            }
            else
            {
                return (vm.DisplayName != null && vm.DisplayName.Contains(trimmedText, StringComparison.OrdinalIgnoreCase)) || (vm.Description != null && vm.Description.Contains(trimmedText, StringComparison.OrdinalIgnoreCase));
            }
        }

        public async Task OnListViewDoubleClick(object sender, RoutedEventArgs e)
        {
            if (!(((FrameworkElement)(e.OriginalSource)).DataContext is SecurityDescriptorTargetViewModel))
            {
                return;
            }

            await this.Edit();
        }

        public void OnGridViewColumnHeaderClick(object sender, RoutedEventArgs e)
        {
            if (!(e.OriginalSource is GridViewColumnHeader gridViewColumnHeader))
            {
                return;
            }

            ListSortDirection newSortDirection = ListSortDirection.Ascending;

            if ((lastHeaderClicked == null || lastHeaderClicked == gridViewColumnHeader) && currentSortDirection == ListSortDirection.Ascending)
            {
                newSortDirection = ListSortDirection.Descending;
            }

            string propertyName = (gridViewColumnHeader.Column.DisplayMemberBinding as Binding)?.Path.Path;
            propertyName ??= gridViewColumnHeader.Column.Header as string;

            SortListView(propertyName, newSortDirection);

            lastHeaderClicked = gridViewColumnHeader;
            currentSortDirection = newSortDirection;

            e.Handled = true;
        }

        private void SortListView(string propertyName, ListSortDirection sortDirection)
        {
            if (propertyName == null)
            {
                return;
            }

            this.Items.SortDescriptions.Clear();

            if (propertyName == nameof(DisplayName))
            {
                if (this.Items.CustomSort == null)
                {
                    this.Items.CustomSort = this.customComparer;
                }

                this.customComparer.SortDirection = sortDirection;
            }
            else
            {
                this.Items.CustomSort = null;
                SortDescription sd = new SortDescription(propertyName, sortDirection);
                this.Items.SortDescriptions.Add(sd);
            }

            this.Items.Refresh();
        }

        public async Task CreatePermissionReport()
        {
            try
            {
                var items = this.selectedItems?.OfType<SecurityDescriptorTargetViewModel>()?.ToList() ?? this.Items.OfType<SecurityDescriptorTargetViewModel>().ToList();

                if (items == null || items.Count == 0)
                {
                    return;
                }

                SaveFileDialog dialog = new SaveFileDialog
                {
                    AddExtension = true,
                    DefaultExt = "csv",
                    OverwritePrompt = true,
                    Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                    Title = "Save permission report"
                };

                if (dialog.ShowDialog() != true)
                {
                    return;
                }

                var records = new List<object>();

                foreach (var target in items)
                {
                    string name;

                    if (target.Target.TryParseAsSid(out SecurityIdentifier sid))
                    {
                        name = sid.ToNtAccountNameOrSidString();
                    }
                    else
                    {
                        name = target.Target;
                    }

                    var aces = target.GetAceEntries();

                    if (aces == null)
                    {
                        continue;
                    }

                    foreach (var ace in aces)
                    {
                        records.Add(new
                        {
                            Target = name,
                            Principal = ace.SecurityIdentifier.ToNtAccountNameOrSidString(),
                            JitAllowed = ace.AceQualifier == System.Security.AccessControl.AceQualifier.AccessAllowed && ((AccessMask)ace.AccessMask).HasFlag(AccessMask.Jit),
                            LapsAllowed = ace.AceQualifier == System.Security.AccessControl.AceQualifier.AccessAllowed && ((AccessMask)ace.AccessMask).HasFlag(AccessMask.LocalAdminPassword),
                            LapsHistoryAllowed = ace.AceQualifier == System.Security.AccessControl.AceQualifier.AccessAllowed && ((AccessMask)ace.AccessMask).HasFlag(AccessMask.LocalAdminPasswordHistory),
                            BitlockerAllowed = ace.AceQualifier == System.Security.AccessControl.AceQualifier.AccessAllowed && ((AccessMask)ace.AccessMask).HasFlag(AccessMask.BitLocker),
                            JitDenied = ace.AceQualifier == System.Security.AccessControl.AceQualifier.AccessDenied && ((AccessMask)ace.AccessMask).HasFlag(AccessMask.Jit),
                            LapsDenied = ace.AceQualifier == System.Security.AccessControl.AceQualifier.AccessDenied && ((AccessMask)ace.AccessMask).HasFlag(AccessMask.LocalAdminPassword),
                            LapsHistoryDenied = ace.AceQualifier == System.Security.AccessControl.AceQualifier.AccessDenied && ((AccessMask)ace.AccessMask).HasFlag(AccessMask.LocalAdminPasswordHistory),
                            BitlockerDenied = ace.AceQualifier == System.Security.AccessControl.AceQualifier.AccessDenied && ((AccessMask)ace.AccessMask).HasFlag(AccessMask.BitLocker),
                        });
                    }
                }

                using (var writer = new StreamWriter(dialog.FileName))
                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    csv.WriteRecords(records);
                }

                await this.dialogCoordinator.ShowMessageAsync(this, "Report saved", "The permission report was successfully created");
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIGenericError, ex, "Unable to generate permission report");
                await this.dialogCoordinator.ShowMessageAsync(this, "The report could not be created", ex.Message);
            }
        }
    }
}
