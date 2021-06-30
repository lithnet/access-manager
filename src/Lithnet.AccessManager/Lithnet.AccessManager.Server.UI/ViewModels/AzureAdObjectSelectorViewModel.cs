using Lithnet.AccessManager.Server.Authorization;
using Lithnet.AccessManager.Server.Configuration;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;
using Stylet;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.Graph;

namespace Lithnet.AccessManager.Server.UI
{
    public class AzureAdObjectSelectorViewModel : ValidatingModelBase
    {
        private readonly IDialogCoordinator dialogCoordinator;
        private readonly ILogger<AzureAdObjectSelectorViewModel> logger;
        private readonly IAadGraphApiProvider graphProvider;
        private ListSortDirection currentSortDirection = ListSortDirection.Ascending;
        private GridViewColumnHeader lastHeaderClicked;
        private HashSet<string> matchedComputerViewModels;
        private IList selectedItems;

        public AzureAdObjectSelectorViewModel(IDialogCoordinator dialogCoordinator, ILogger<AzureAdObjectSelectorViewModel> logger, IAadGraphApiProvider graphProvider, IModelValidator<AzureAdObjectSelectorViewModel> validator) : base(validator)
        {
            this.dialogCoordinator = dialogCoordinator;
            this.logger = logger;
            this.graphProvider = graphProvider;
            this.Items = new BindableCollection<object>();
            this.Validate();
        }

        public void SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.selectedItems ??= (e.Source as ListView)?.SelectedItems;
            this.Validate();
        }

        public bool IsFilterApplied { get; set; }

        public bool IsNotSearching => !this.IsSearching;

        public bool IsSearching { get; set; }

        public BindableCollection<object> Items { get; private set; }

        public string SearchText { get; set; }

        public object SelectedItem { get; set; }

        public TargetType Type { get; set; }

        public bool CanApplySearchFilter => !string.IsNullOrWhiteSpace(this.SearchText);

        public string TenantId { get; set; }

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
                        this.Items.Clear();

                        if (this.Type == TargetType.AadGroup)
                        {
                            await foreach (var group in this.graphProvider.FindGroups(this.TenantId, this.SearchText))
                            {
                                this.Items.Add(group);
                            }
                        }
                        else if (this.Type == TargetType.AadComputer)
                        {
                            await foreach (var device in this.graphProvider.FindDevices(this.TenantId, this.SearchText))
                            {
                                this.Items.Add(device);
                            }
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
            this.Items.Clear();
            this.Items.Refresh();
        }

        public async Task SearchTextBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && this.CanApplySearchFilter)
            {
                e.Handled = true;
                await this.ApplySearchFilter();
            }
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

            //await this.Edit();
        }

        public void OnGridViewColumnHeaderClick(object sender, RoutedEventArgs e)
        {
            if (!(e.OriginalSource is GridViewColumnHeader gridViewColumnHeader))
            {
                return;
            }

            if (gridViewColumnHeader.Column == null)
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
            //if (propertyName == null)
            //{
            //    return;
            //}

            //this.Items.SortDescriptions.Clear();

            //if (propertyName == nameof(DisplayName))
            //{
            //    //if (this.Items.CustomSort == null)
            //    //{
            //    //    this.Items.CustomSort = this.customComparer;
            //    //}

            //    //this.customComparer.SortDirection = sortDirection;
            //}
            //else
            //{
            //    this.Items.CustomSort = null;
            //    SortDescription sd = new SortDescription(propertyName, sortDirection);
            //    this.Items.SortDescriptions.Add(sd);
            //}

            this.Items.Refresh();
        }
    }
}
