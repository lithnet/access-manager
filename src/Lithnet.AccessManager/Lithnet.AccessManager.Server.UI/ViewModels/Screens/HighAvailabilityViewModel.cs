using Lithnet.AccessManager.Server.Configuration;
using Lithnet.AccessManager.Server.Providers;
using Lithnet.AccessManager.Server.UI.Providers;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.IconPacks;
using Microsoft.Extensions.Logging;
using Stylet;
using System;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Server.UI
{
    public class HighAvailabilityViewModel : Screen, IHelpLink
    {
        private readonly IDialogCoordinator dialogCoordinator;
        private readonly IShellExecuteProvider shellExecuteProvider;
        private readonly ILogger<HighAvailabilityViewModel> logger;
        private readonly DataProtectionOptions dataProtectionOptions;
        private readonly ICertificateSynchronizationProvider certSyncProvider;
        private readonly ISecretRekeyProvider rekeyProvider;
        private readonly IScriptTemplateProvider scriptTemplateProvider;
        private readonly IWindowsServiceProvider windowsServiceProvider;

        public HighAvailabilityViewModel(IDialogCoordinator dialogCoordinator, IShellExecuteProvider shellExecuteProvider, ILogger<HighAvailabilityViewModel> logger, INotifyModelChangedEventPublisher eventPublisher, DataProtectionOptions dataProtectionOptions, ICertificateSynchronizationProvider certSyncProvider, ISecretRekeyProvider rekeyProvider, IScriptTemplateProvider scriptTemplateProvider, IWindowsServiceProvider windowsServiceProvider, IViewModelFactory<EnterpriseEditionBannerViewModel, EnterpriseEditionBannerModel> enterpriseEditionViewModelFactory)
        {
            this.shellExecuteProvider = shellExecuteProvider;
            this.logger = logger;
            this.dataProtectionOptions = dataProtectionOptions;
            this.certSyncProvider = certSyncProvider;
            this.dialogCoordinator = dialogCoordinator;
            this.rekeyProvider = rekeyProvider;
            this.scriptTemplateProvider = scriptTemplateProvider;
            this.windowsServiceProvider = windowsServiceProvider;

            this.DisplayName = "High availability";
            eventPublisher.Register(this);

            this.isClusterCompatibleSecretEncryptionEnabled = this.dataProtectionOptions.EnableClusterCompatibleSecretEncryption;

            this.EnterpriseEdition = enterpriseEditionViewModelFactory.CreateViewModel(new EnterpriseEditionBannerModel
            {
                RequiredFeature = Enterprise.LicensedFeatures.HighAvailability,
                Link = Constants.EnterpriseEditionLearnMoreLinkHa
            });
        }

        public EnterpriseEditionBannerViewModel EnterpriseEdition { get; set; }

        public string HelpLink => Constants.HelpLinkPageHighAvailability;

        public PackIconMaterialKind Icon => PackIconMaterialKind.Server;

        public async Task Help()
        {
            await this.shellExecuteProvider.OpenWithShellExecute(this.HelpLink);
        }

        [NotifyModelChangedProperty]
        public bool IsCertificateSynchronizationEnabled
        {
            get => this.dataProtectionOptions.EnableCertificateSynchronization;
            set => this.dataProtectionOptions.EnableCertificateSynchronization = value;
        }


        private bool isClusterCompatibleSecretEncryptionEnabled;

        [NotifyModelChangedProperty]
        public bool IsClusterCompatibleSecretEncryptionEnabled
        {
            get => isClusterCompatibleSecretEncryptionEnabled;
            set => isClusterCompatibleSecretEncryptionEnabled = value;
        }

        public void OnIsClusterCompatibleSecretEncryptionEnabledChanged()
        {
            _ = Task.Run(async () =>
              {
                  try
                  {
                      var previous = this.dataProtectionOptions.EnableClusterCompatibleSecretEncryption;
                      this.dataProtectionOptions.EnableClusterCompatibleSecretEncryption = this.isClusterCompatibleSecretEncryptionEnabled;

                      var result = await this.rekeyProvider.TryReKeySecretsAsync(this);

                      if (!result)
                      {
                          this.isClusterCompatibleSecretEncryptionEnabled = previous;
                          this.dataProtectionOptions.EnableClusterCompatibleSecretEncryption = previous;
                          this.NotifyOfPropertyChange(nameof(IsClusterCompatibleSecretEncryptionEnabled));
                      }
                  }
                  catch (Exception ex)
                  {
                      logger.LogError(EventIDs.UIGenericError, ex, "Could not re-key secrets");
                      await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Unable to re-key the application secrets\r\n{ex.Message}");
                  }
              });
        }


        public async Task SynchronizeSecrets()
        {
            try
            {
                this.certSyncProvider.ExportCertificatesToConfig();
                this.certSyncProvider.ImportCertificatesFromConfig();
            }
            catch (Exception ex)
            {
                logger.LogError(EventIDs.UIGenericError, ex, "Could not synchronize secrets");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Unable to complete the synchronization process\r\n{ex.Message}");
            }
        }

        public async Task GetDatabaseCreationScript()
        {
            try
            {
                var vm = new ScriptContentViewModel(this.dialogCoordinator)
                {
                    DefaultExt = "sql",
                    Filter = "SQL script (*.sql)|*.sql",
                    HelpText = "Run the following script on the database server to create the SQL database",
                    ScriptText = this.scriptTemplateProvider.CreateDatabase
                        .Replace("{serviceAccount}", this.windowsServiceProvider.GetServiceNTAccount().Value, StringComparison.OrdinalIgnoreCase)
                };

                ExternalDialogWindow w = new ExternalDialogWindow
                {
                    Title = "Script",
                    DataContext = vm,
                    SaveButtonVisible = false,
                    CancelButtonName = "Close"
                };

                w.ShowDialog();
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIGenericError, ex, "Could not complete the operation");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Could not complete the operation\r\n{ex.Message}");
            }
        }
    }
}
