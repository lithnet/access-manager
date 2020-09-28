using System;
using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Management.Automation.Language;
using System.Security.Principal;
using System.Threading.Tasks;
using Lithnet.AccessManager.Server.UI.AuthorizationRuleImport;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public sealed class ImportWizardImportContainerViewModel : Screen
    {
        private readonly ILogger<ImportWizardImportContainerViewModel> logger;
        private readonly IDialogCoordinator dialogCoordinator;
        private readonly IDiscoveryServices discoveryServices;
        private readonly IObjectSelectionProvider objectSelectionProvider;
        private readonly IDirectory directory;
        private readonly IServiceSettingsProvider serviceSettings;

        public Task Initialization { get; private set; }

        public ImportWizardImportContainerViewModel(ILogger<ImportWizardImportContainerViewModel> logger, IDialogCoordinator dialogCoordinator, IModelValidator<ImportWizardImportContainerViewModel> validator, IObjectSelectionProvider objectSelectionProvider, IDiscoveryServices discoveryServices, IDirectory directory, IServiceSettingsProvider serviceSettings)
        {
            this.logger = logger;
            this.dialogCoordinator = dialogCoordinator;
            this.discoveryServices = discoveryServices;
            this.objectSelectionProvider = objectSelectionProvider;
            this.directory = directory;
            this.Validator = validator;
            this.serviceSettings = serviceSettings;
            this.Initialization = this.Initialize();
        }

        public async Task Initialize()
        {
            var serviceAccount = this.serviceSettings.GetServiceAccount();
            if (serviceAccount != null)
            {
                this.FilteredSids.Add(new SecurityIdentifierViewModel(serviceAccount, directory));
            }

            await this.ValidateAsync();
        }

        public string Target { get; set; }

        public bool DoNotConsolidate { get; set; }

        public bool DoNotConsolidateOnError { get; set; }

        public bool DoNotConsolidateOnErrorEnabled => !this.DoNotConsolidate;

        public bool DoNotConsolidateOnErrorVisible { get; set; }

        public string DoNotConsolidateOnErrorText { get; set; }

        public string ContainerHelperText { get; set; }

        public SecurityIdentifierViewModel SelectedFilteredSid { get; set; }

        public BindableCollection<SecurityIdentifierViewModel> FilteredSids { get; } = new BindableCollection<SecurityIdentifierViewModel>();

        public BindableCollection<SecurityIdentifierViewModel> FilteredComputers { get; } = new BindableCollection<SecurityIdentifierViewModel>();


        public async Task AddFilteredComputer()
        {
            try
            {
                if (this.objectSelectionProvider.GetComputers(this, out List<SecurityIdentifier> sids))
                {
                    foreach (SecurityIdentifier sid in sids)
                    {
                        SecurityIdentifierViewModel sidvm = await Task.Run(() => new SecurityIdentifierViewModel(sid, directory));

                        if (this.FilteredComputers.Any(t => string.Equals(t.Sid, sidvm.Sid, StringComparison.OrdinalIgnoreCase)))
                        {
                            continue;
                        }

                        this.FilteredComputers.Add(sidvm);
                    }
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIGenericError, ex, "Select group error");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"An error occurred when processing the request\r\n{ex.Message}");
            }
        }


        public async Task DeleteFilteredComputer(System.Collections.IList items)
        {
            if (items == null)
            {
                return;
            }

            var itemsToDelete = items.Cast<SecurityIdentifierViewModel>().ToList();

            MetroDialogSettings s = new MetroDialogSettings
            {
                AnimateShow = false,
                AnimateHide = false
            };

            string message = itemsToDelete.Count == 1 ? "Are you sure you want to delete this item?" : $"Are you sure you want to delete {itemsToDelete.Count} items?";

            if (await this.dialogCoordinator.ShowMessageAsync(this, "Confirm", message, MessageDialogStyle.AffirmativeAndNegative, s) == MessageDialogResult.Affirmative)
            {
                foreach (var deleting in itemsToDelete)
                {
                    this.FilteredComputers.Remove(deleting);
                }
            }
        }

        public async Task AddFilteredSid()
        {
            try
            {
                if (this.objectSelectionProvider.GetUserOrGroups(this, out List<SecurityIdentifier> sids))
                {
                    foreach (SecurityIdentifier sid in sids)
                    {
                        SecurityIdentifierViewModel sidvm = await Task.Run(() => new SecurityIdentifierViewModel(sid, directory));

                        if (this.FilteredSids.Any(t => string.Equals(t.Sid, sidvm.Sid, StringComparison.OrdinalIgnoreCase)))
                        {
                            continue;
                        }

                        this.FilteredSids.Add(sidvm);
                    }
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIGenericError, ex, "Select group error");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"An error occurred when processing the request\r\n{ex.Message}");
            }
        }

        public async Task DeleteFilteredSid(System.Collections.IList items)
        {
            if (items == null)
            {
                return;
            }

            var itemsToDelete = items.Cast<SecurityIdentifierViewModel>().ToList();

            MetroDialogSettings s = new MetroDialogSettings
            {
                AnimateShow = false,
                AnimateHide = false
            };

            string message = itemsToDelete.Count == 1 ? "Are you sure you want to delete this item?" : $"Are you sure you want to delete {itemsToDelete.Count} items?";

            if (await this.dialogCoordinator.ShowMessageAsync(this, "Confirm", message, MessageDialogStyle.AffirmativeAndNegative, s) == MessageDialogResult.Affirmative)
            {
                foreach (var deleting in itemsToDelete)
                {
                    this.FilteredSids.Remove(deleting);
                }
            }
        }

        public void SetImportMode(ImportMode mode)
        {
            switch (mode)
            {
                case ImportMode.BitLocker:
                case ImportMode.Laps:
                    this.ContainerHelperText = "Select the container that contains the permissions you want to import";
                    this.DoNotConsolidateOnErrorVisible = false;
                    this.DoNotConsolidateOnErrorText = null;
                    this.DoNotConsolidateOnError = false;

                    break;

                case ImportMode.Rpc:
                    this.ContainerHelperText = "Select the container that contains the computers you want to import the local admin membership from";
                    this.DoNotConsolidateOnErrorVisible = true;
                    this.DoNotConsolidateOnErrorText = "Do not consolidate common permissions for a given OU if any of its child computers are not contactable. (If one or more computers in an OU cannot be processed, an individual access rule will be created for all computers in that OU";
                    break;

                case ImportMode.CsvFile:
                    this.ContainerHelperText = "Select the container that contains the computers contained in the import file. The OU structure will be used to build the permission structure, and consolidate as many permissions as possible into OU-level authorization rules";
                    this.DoNotConsolidateOnErrorVisible = true;
                    this.DoNotConsolidateOnErrorText = "Do not consolidate common permissions for a given OU if any of its child computers are missing from the import file. (If one or more computers in an OU cannot be processed, an individual access rule will be created for all computers in that OU";
                    break;
            }
        }

        public async Task SelectTarget()
        {
            try
            {
                ShowContainerDialog();
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIGenericError, ex, "Select target error");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"An error occurred when processing the request\r\n{ex.Message}");
            }
        }

        private void ShowContainerDialog()
        {
            string path = this.Target ?? Domain.GetComputerDomain().GetDirectoryEntry().GetPropertyString("distinguishedName");

            string basePath = this.discoveryServices.GetFullyQualifiedRootAdsPath(path);
            string initialPath = this.discoveryServices.GetFullyQualifiedAdsPath(path);

            if (this.objectSelectionProvider.SelectContainer(this, "Select container", "Select container", basePath, initialPath, out string container))
            {
                this.Target = container;
            }
        }
    }
}
