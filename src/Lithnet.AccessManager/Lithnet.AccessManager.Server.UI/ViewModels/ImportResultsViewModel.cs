using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using CsvHelper;
using Lithnet.AccessManager.Server.Configuration;
using Lithnet.AccessManager.Server.UI.AuthorizationRuleImport;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using PropertyChanged;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public sealed class ImportResultsViewModel : Screen
    {
        private readonly ImportResults results;
        private readonly ISecurityDescriptorTargetsViewModelFactory targetsFactory;
        private readonly IEventAggregator eventAggregator;
        private readonly ILogger<ImportResultsViewModel> logger;
        private readonly IDialogCoordinator dialogCoordinator;
        private ListCollectionView errorItems;

        public Task Initialization { get; private set; }

        public ImportResultsViewModel(ImportResults results, ISecurityDescriptorTargetsViewModelFactory targetsFactory, IEventAggregator eventAggregator, ILogger<ImportResultsViewModel> logger, IDialogCoordinator dialogCoordinator)
        {
            this.results = results;
            this.logger = logger;
            this.dialogCoordinator = dialogCoordinator;
            this.targetsFactory = targetsFactory;
            this.eventAggregator = eventAggregator;
            this.Initialization = this.Initialize();
        }

        private async Task Initialize()
        {
            this.Targets = await targetsFactory.CreateViewModelAsync(results.Targets);
            this.Targets.ChildDisplaySettings.IsScriptVisible = false;
            this.Targets.ViewModels.CollectionChanged += ViewModels_CollectionChanged;

            this.errorItems = (ListCollectionView)CollectionViewSource.GetDefaultView(this.DiscoveryErrors);
            this.errorItems.Filter = x =>
            {
                DiscoveryError e = (DiscoveryError)x;
                if (e.IsError && this.ShowErrors)
                {
                    return true;
                }

                if (e.IsInformational && this.ShowInformational)
                {
                    return true;
                }

                if (e.IsWarning && this.ShowWarnings)
                {
                    return true;
                }

                return false;
            };

            this.PublishNotificationChannels();
        }

        private void ViewModels_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            this.NotifyOfPropertyChange(nameof(TargetCount));
        }

        public NotificationChannels NotificationChannels => this.results.NotificationChannels;

        public SecurityDescriptorTargetsViewModel Targets { get; private set; }

        public bool Merge { get; set; } = true;

        public bool MergeOverwrite { get; set; }

        public bool HasDiscoveryErrors => DiscoveryErrors.Count > 0;

        [OnChangedMethod(nameof(RefreshFilter))]
        public bool ShowErrors { get; set; } = true;

        [OnChangedMethod(nameof(RefreshFilter))]
        public bool ShowWarnings { get; set; } = true;

        [OnChangedMethod(nameof(RefreshFilter))]
        public bool ShowInformational { get; set; } = true;

        private void RefreshFilter()
        {
            this.errorItems.Refresh();
        }

        public List<DiscoveryError> DiscoveryErrors => this.results.DiscoveryErrors;

        public string DiscoveryErrorCount => BuildDiscoveryIssueCountString();

        public string TargetCount => $"{Targets.ViewModels.Count} rule{(Targets.ViewModels.Count == 1 ? "" : "s")} found";

        protected override void OnDeactivate()
        {
            this.UnpublishNotificationChannels();
            base.OnDeactivate();
        }

        private void PublishNotificationChannels()
        {
            foreach (SmtpNotificationChannelDefinition channel in this.NotificationChannels.Smtp)
            {
                this.eventAggregator.Publish(new NotificationSubscriptionChangedEvent { IsTransient = true, ModificationType = ModificationType.Added, ModifiedObject = channel });
            }

            foreach (WebhookNotificationChannelDefinition channel in this.NotificationChannels.Webhooks)
            {
                this.eventAggregator.Publish(new NotificationSubscriptionChangedEvent { IsTransient = true, ModificationType = ModificationType.Added, ModifiedObject = channel });
            }

            foreach (PowershellNotificationChannelDefinition channel in this.NotificationChannels.Powershell)
            {
                this.eventAggregator.Publish(new NotificationSubscriptionChangedEvent { IsTransient = true, ModificationType = ModificationType.Added, ModifiedObject = channel });
            }
        }

        private void UnpublishNotificationChannels()
        {
            foreach (SmtpNotificationChannelDefinition channel in this.NotificationChannels.Smtp)
            {
                this.eventAggregator.Publish(new NotificationSubscriptionChangedEvent { IsTransient = true, ModificationType = ModificationType.Deleted, ModifiedObject = channel });
            }

            foreach (WebhookNotificationChannelDefinition channel in this.NotificationChannels.Webhooks)
            {
                this.eventAggregator.Publish(new NotificationSubscriptionChangedEvent { IsTransient = true, ModificationType = ModificationType.Deleted, ModifiedObject = channel });
            }

            foreach (PowershellNotificationChannelDefinition channel in this.NotificationChannels.Powershell)
            {
                this.eventAggregator.Publish(new NotificationSubscriptionChangedEvent { IsTransient = true, ModificationType = ModificationType.Deleted, ModifiedObject = channel });
            }
        }

        private string BuildDiscoveryIssueCountString()
        {
            int errors = DiscoveryErrors.Count(t => t.IsError);
            int warnings = DiscoveryErrors.Count(t => t.IsWarning);
            int info = DiscoveryErrors.Count(t => t.IsInformational);

            if (errors + warnings + info == 0)
            {
                return null;
            }

            List<string> components = new List<string>();

            if (errors > 0)
            {
                components.Add($"{errors} error{(errors == 1 ? "" : "s")}");
            }

            if (warnings > 0)
            {
                components.Add($"{warnings} warning{(warnings == 1 ? "" : "s")}");
            }

            if (info > 0)
            {
                components.Add($"{info} informational");
            }

            return string.Join(", ", components);
        }

        public async Task Export()
        {
            try
            {
                SaveFileDialog dialog = new SaveFileDialog();
                dialog.CheckFileExists = false;
                dialog.CheckPathExists = true;
                dialog.DefaultExt = "csv";
                dialog.DereferenceLinks = true;
                dialog.Filter = "CSV file (*.csv)|*.csv";

                if (dialog.ShowDialog(this.GetWindow()) == false)
                {
                    return;
                }

                await using (StreamWriter writer = new StreamWriter(dialog.FileName))
                {
                    await using (CsvWriter csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                    {
                        await csv.WriteRecordsAsync(this.DiscoveryErrors);
                    }
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIGenericError, ex, "Unable to save file");
                await dialogCoordinator.ShowMessageAsync(this, "File save error", $"There was an error saving the data\r\n\r\n{ex.Message}");
            }
        }
    }
}
