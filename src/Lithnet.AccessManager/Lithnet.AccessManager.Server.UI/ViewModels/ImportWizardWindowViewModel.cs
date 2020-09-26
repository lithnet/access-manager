using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lithnet.AccessManager.Server.UI.AuthorizationRuleImport;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;
using PropertyChanged;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class ImportWizardWindowViewModel : Conductor<PropertyChangedBase>.Collection.OneActive
    {
        private readonly IDialogCoordinator dialogCoordinator;
        private readonly ILogger<ImportWizardWindowViewModel> logger;
        private readonly ImportWizardCsvSettingsViewModel csvSettingsVm;
        private readonly ImportWizardImportContainerViewModel containerVm;
        private readonly ImportWizardImportTypeViewModel importTypeVm;
        private readonly ImportWizardRuleSettingsViewModel ruleVm;
        private readonly ImportWizardLapsWebSettingsViewModel lapsWebVm;
        private readonly ImportWizardImportReadyViewModel importReadyVm;
        private readonly SecurityDescriptorTargetsViewModelFactory targetFactory;
        private readonly IImportProviderFactory importProviderFactory;

        private ProgressDialogController progress;
        private int progressCurrent;
        private int progressMaximum;
        private CancellationTokenSource progressCts;

        public bool NextButtonIsDefault { get; set; } = true;

        public bool CancelButtonVisible { get; set; } = true;

        public bool NextButtonVisible { get; set; } = true;

        public bool BackButtonVisible { get; set; } = true;

        public bool SaveButtonVisible { get; set; } = false;

        public bool SaveButtonIsDefault { get; set; } = false;

        public bool ImportButtonVisible { get; set; } = false;

        public bool ImportButtonIsDefault { get; set; } = false;

        public AuthorizationViewModel AuthorizationViewModel { get; set; }

        public ImportWizardWindowViewModel(IDialogCoordinator dialogCoordinator, ILogger<ImportWizardWindowViewModel> logger, ImportWizardImportTypeViewModel importTypeVm, ImportWizardCsvSettingsViewModel csvSettingsVm, ImportWizardImportContainerViewModel containerVm, ImportWizardRuleSettingsViewModel ruleVm, ImportWizardLapsWebSettingsViewModel lapsWebVm, ImportWizardImportReadyViewModel importReadyVm, SecurityDescriptorTargetsViewModelFactory targetFactory, IImportProviderFactory importProviderFactory)
        {
            this.logger = logger;
            this.dialogCoordinator = dialogCoordinator;
            this.DisplayName = Constants.AppName;
            this.dialogCoordinator = dialogCoordinator;
            this.targetFactory = targetFactory;
            this.importProviderFactory = importProviderFactory;

            // VM mappings
            this.importTypeVm = importTypeVm;
            this.csvSettingsVm = csvSettingsVm;
            this.containerVm = containerVm;
            this.ruleVm = ruleVm;
            this.lapsWebVm = lapsWebVm;
            this.importReadyVm = importReadyVm;

            // Bindings
            this.importTypeVm.Bind(t => t.ImportType, OnImportModeChanged);

            // Initial binds
            this.Items.Add(importTypeVm);
            this.ActiveItem = this.Items.First();
        }

        [SuppressPropertyChangedWarnings]
        private void OnImportModeChanged(object x, PropertyChangedExtendedEventArgs<ImportMode> y)
        {
            this.containerVm.SetImportMode(this.importTypeVm.ImportType);
            this.ruleVm.SetImportMode(this.importTypeVm.ImportType);
            this.BuildPages(this.importTypeVm.ImportType);
        }

        public override void ActivateItem(PropertyChangedBase item)
        {
            base.ActivateItem(item);

            if (item is INotifyDataErrorInfo e)
            {
                e.ErrorsChanged += E_ErrorsChanged;
            }

            this.UpdateNavigationCapabilities();
        }
        
        public override void DeactivateItem(PropertyChangedBase item)
        {
            base.DeactivateItem(item);

            if (item is INotifyDataErrorInfo e)
            {
                e.ErrorsChanged -= E_ErrorsChanged;
            }

            if (item is ImportResultsViewModel)
            {
                this.Items.Remove(item);
            }

            this.UpdateNavigationCapabilities();
        }
        
        private void E_ErrorsChanged(object sender, DataErrorsChangedEventArgs e)
        {
            this.UpdateNavigationCapabilities();
        }

        public bool CanNext
        {
            get
            {
                if (this.ActiveItem is INotifyDataErrorInfo vm && vm.HasErrors)
                {
                    return false;
                }

                return this.Items.IndexOf(this.ActiveItem) < this.Items.Count - 1;
            }
        }

        public void Next()
        {
            var oldItem = this.ActiveItem;

            int index = this.Items.IndexOf(this.ActiveItem);
            index++;

            if (index < this.Items.Count)
            {
                this.ActiveItem = this.Items[index];
            }

            if (oldItem != null && oldItem != this.ActiveItem)
            {
                this.DeactivateItem(oldItem);
            }

            this.UpdateNavigationCapabilities();
        }

        public bool CanBack => this.Items.IndexOf(this.ActiveItem) > 0;

        public void Back()
        {
            var oldItem = this.ActiveItem;

            int index = this.Items.IndexOf(this.ActiveItem);
            index--;

            if (index >= 0)
            {
                this.ActiveItem = this.Items[index];
            }

            if (oldItem != null && oldItem != this.ActiveItem)
            {
                this.DeactivateItem(oldItem);
            }

            this.UpdateNavigationCapabilities();
        }

        public async Task Cancel()
        {
            if (await this.dialogCoordinator.ShowMessageAsync(this, "Cancel", "Are you sure you want to cancel the import process?", MessageDialogStyle.AffirmativeAndNegative) == MessageDialogResult.Affirmative)
            {
                this.RequestClose(false);
            }
        }

        public async Task Save()
        {
            try
            {
                if (!(this.ActiveItem is ImportResultsViewModel irvm))
                {
                    return;
                }

                this.AuthorizationViewModel.Merge(irvm.Targets, irvm.Merge, irvm.MergeOverwrite);
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIGenericError, ex, "Merge error");
                await this.dialogCoordinator.ShowMessageAsync(this, "Import error", $"Could not merge the new rules into the authorization store. {ex.Message}");
            }
        }

        public async Task Import()
        {
            try
            {
                this.progress = await this.dialogCoordinator.ShowProgressAsync(this, "Importing...", "Discovering import objects", true);
                this.progress.Canceled += Progress_Canceled;
                this.progress.SetIndeterminate();
                this.progressCts = new CancellationTokenSource();
                this.progressCurrent = 0;

                ImportSettings settings = this.GetImportSettings();
                settings.CancellationToken = this.progressCts.Token;
                IImportProvider provider = this.importProviderFactory.CreateImportProvider(settings);
                provider.OnItemProcessStart += ImportProvider_OnStartProcessingComputer;

                await Task.Run(() =>
                {
                    this.progressMaximum = provider.GetEstimatedItemCount();
                    this.progress.Maximum = this.progressMaximum;
                });


                var results = await Task.Run(() => provider.Import());

                this.progress.SetIndeterminate();
                this.progress.SetMessage("Building authorization rules...");

                ImportResultsViewModel irvm = new ImportResultsViewModel();
                irvm.Targets = targetFactory.CreateViewModel(results.Targets);
                irvm.DiscoveryErrors = results.DiscoveryErrors;
                irvm.Targets.ChildDisplaySettings.IsScriptVisible = false;

                await this.progress.CloseAsync();

                this.Items.Add(irvm);
                this.ActiveItem = irvm;
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIGenericError, ex, "Import error");
                await this.dialogCoordinator.ShowMessageAsync(this, "Import error", $"Could not perform the import. {ex.Message}");
            }
            finally
            {
                if (this.progress.IsOpen)
                {
                    await this.progress.CloseAsync();
                }
            }
        }


        private ImportSettings GetImportSettings()
        {
            ImportSettings importSettings;

            switch (this.importTypeVm.ImportType)
            {
                case ImportMode.CsvFile:
                    importSettings = new ImportSettingsCsv()
                    {
                        ImportFile = this.csvSettingsVm.ImportFile,
                        HasHeaderRow = this.csvSettingsVm.ImportFileHasHeaderRow
                    };
                    break;

                case ImportMode.Rpc:
                    importSettings = new ImportSettingsRpc();
                    break;

                case ImportMode.BitLocker:
                    importSettings = new ImportSettingsBitLocker();
                    break;

                case ImportMode.LapsWeb:
                    importSettings = new ImportSettingsLapsWeb
                    {
                        ImportFile = this.lapsWebVm.ImportFile
                    };
                    break;

                default:
                    throw new InvalidOperationException();
            }

            if (importSettings is ImportSettingsComputerDiscovery cd)
            {
                cd.ImportOU = this.containerVm.Target;
                cd.DoNotConsolidate = this.containerVm.DoNotConsolidate;
                cd.DoNotConsolidateOnError = this.containerVm.DoNotConsolidateOnError;
            }

            importSettings.ImportMode = this.importTypeVm.ImportType;
            importSettings.AllowBitLocker = this.ruleVm.AllowBitlocker;
            importSettings.AllowJit = this.ruleVm.AllowJit;
            importSettings.AllowLaps = this.ruleVm.AllowLaps;
            importSettings.AllowLapsHistory = this.ruleVm.AllowLapsHistory;
            importSettings.JitAuthorizingGroup = this.ruleVm.JitAuthorizingGroup;
            importSettings.JitExpireAfter = this.ruleVm.JitExpireAfter;
            importSettings.Notifications = this.ruleVm.Notifications?.Model;
            importSettings.RuleDescription = this.ruleVm.Description;

            importSettings.PrincipalFilter = new List<System.Security.Principal.SecurityIdentifier>();


            foreach (var sid in this.containerVm.FilteredSids)
            {
                importSettings.PrincipalFilter.Add(sid.SecurityIdentifier);
            }

            return importSettings;
        }


        private void BuildPages(ImportMode mode)
        {
            this.RemoveWizardPages();

            switch (mode)
            {
                case ImportMode.CsvFile:
                    this.Items.Add(csvSettingsVm);
                    this.Items.Add(containerVm);
                    this.Items.Add(ruleVm);
                    this.Items.Add(importReadyVm);
                    break;

                case ImportMode.BitLocker:
                    this.Items.Add(containerVm);
                    this.Items.Add(ruleVm);
                    this.Items.Add(importReadyVm);
                    break;

                case ImportMode.Laps:
                    this.Items.Add(containerVm);
                    this.Items.Add(ruleVm);
                    this.Items.Add(importReadyVm);
                    break;

                case ImportMode.Rpc:
                    this.Items.Add(containerVm);
                    this.Items.Add(ruleVm);
                    this.Items.Add(importReadyVm);

                    break;

                case ImportMode.LapsWeb:
                    this.Items.Add(lapsWebVm);
                    this.Items.Add(ruleVm);
                    this.Items.Add(importReadyVm);
                    break;
            }

            this.UpdateNavigationCapabilities();
        }

        private void UpdateNavigationCapabilities()
        {
            if (this.ActiveItem is ImportWizardImportReadyViewModel)
            {
                this.NextButtonIsDefault = false;
                this.NextButtonVisible = false;
                this.SaveButtonIsDefault = false;
                this.SaveButtonVisible = false;
                this.ImportButtonVisible = true;
                this.ImportButtonIsDefault = true;
            }
            else if (this.ActiveItem is ImportResultsViewModel)
            {
                this.NextButtonIsDefault = false;
                this.NextButtonVisible = false;
                this.SaveButtonIsDefault = true;
                this.SaveButtonVisible = true;
                this.ImportButtonVisible = false;
                this.ImportButtonIsDefault = false;
            }
            else
            {
                this.NextButtonIsDefault = true;
                this.NextButtonVisible = true;
                this.SaveButtonIsDefault = false;
                this.SaveButtonVisible = false;
                this.ImportButtonVisible = false;
                this.ImportButtonIsDefault = false;
            }

            this.NotifyOfPropertyChange(nameof(this.CanNext));
            this.NotifyOfPropertyChange(nameof(this.CanBack));
        }

        private void RemoveWizardPages()
        {
            this.Items.RemoveRange(this.Items.Except(new[] { this.importTypeVm }).ToList());
        }

        private void Progress_Canceled(object sender, EventArgs e)
        {
            this.progressCts?.Cancel();
        }

        private void ImportProvider_OnStartProcessingComputer(object sender, ImportProcessingEventArgs e)
        {
            this.progress.SetMessage(e.Message);
            var val = Interlocked.Increment(ref this.progressCurrent);
            this.logger.LogTrace("Progress {count}/{max}", val, this.progressMaximum);
            this.progress.SetProgress(Math.Min(val, this.progressMaximum));
        }
    }
}
