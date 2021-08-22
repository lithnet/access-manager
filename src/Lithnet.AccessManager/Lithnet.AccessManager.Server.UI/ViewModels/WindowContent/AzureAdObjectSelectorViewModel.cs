using Lithnet.AccessManager.Server.Configuration;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;
using Stylet;
using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Lithnet.AccessManager.Server.UI
{
    public class AzureAdObjectSelectorViewModel : Screen, IExternalDialogAware
    {
        private readonly IDialogCoordinator dialogCoordinator;
        private readonly ILogger<AzureAdObjectSelectorViewModel> logger;
        private readonly IAadGraphApiProvider graphProvider;

        public AzureAdObjectSelectorViewModel(IDialogCoordinator dialogCoordinator, ILogger<AzureAdObjectSelectorViewModel> logger, IAadGraphApiProvider graphProvider, IModelValidator<AzureAdObjectSelectorViewModel> validator) : base(validator)
        {
            this.dialogCoordinator = dialogCoordinator;
            this.logger = logger;
            this.graphProvider = graphProvider;
            this.Items = new BindableCollection<object>();
            this.Validate();
            this.DisplayName = "Select an Azure AD object";
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

        public void OnTypeChanged()
        {
            if (this.Type == TargetType.AadComputer)
            {
                this.DisplayName = "Select an Azure AD computer";
            }
            else if (this.Type == TargetType.AadGroup)
            {
                this.DisplayName = "Select an Azure AD group";
            }
            else
            {
                this.DisplayName = "Select an Azure AD object";
            }
        }

        public bool CancelButtonVisible { get; set; } = true;

        public bool SaveButtonVisible { get; set; } = true;

        public bool CancelButtonIsDefault { get; set; } = false;

        public bool SaveButtonIsDefault { get; set; } = true;

        public string SaveButtonName { get; set; } = "Select...";

        public string CancelButtonName { get; set; } = "Cancel";
    }
}
