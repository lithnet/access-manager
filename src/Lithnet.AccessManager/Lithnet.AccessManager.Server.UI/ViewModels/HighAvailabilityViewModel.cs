using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Lithnet.AccessManager.Enterprise;
using Lithnet.AccessManager.Server.Configuration;
using Lithnet.AccessManager.Server.UI.Interop;
using Lithnet.AccessManager.Server.UI.Providers;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.IconPacks;
using Markdig.Extensions.TaskLists;
using Microsoft.Extensions.Logging;
using PropertyChanged;
using Stylet;
using Vanara.PInvoke;

namespace Lithnet.AccessManager.Server.UI
{
    public class HighAvailabilityViewModel : Screen, IHelpLink
    {
        private readonly IDialogCoordinator dialogCoordinator;
        private readonly IShellExecuteProvider shellExecuteProvider;
        private readonly ILicenseManager licenseManager;
        private readonly ILogger<HighAvailabilityViewModel> logger;
        private readonly HighAvailabilityOptions highAvailabilityOptions;
        private readonly IProtectedSecretProvider secretProvider;
        private readonly ILicenseDataProvider licenseProvider;
        private readonly DataProtectionOptions dataProtectionOptions;
        private readonly ICertificateSynchronizationProvider certSyncProvider;
        private readonly ISecretRekeyProvider rekeyProvider;

        public HighAvailabilityViewModel(IDialogCoordinator dialogCoordinator, IShellExecuteProvider shellExecuteProvider, ILicenseManager licenseManager, ILogger<HighAvailabilityViewModel> logger, INotifyModelChangedEventPublisher eventPublisher, HighAvailabilityOptions highAvailabilityOptions, IProtectedSecretProvider secretProvider, ILicenseDataProvider licenseProvider, DataProtectionOptions dataProtectionOptions, ICertificateSynchronizationProvider certSyncProvider, ISecretRekeyProvider rekeyProvider)
        {
            this.shellExecuteProvider = shellExecuteProvider;
            this.licenseManager = licenseManager;
            this.logger = logger;
            this.highAvailabilityOptions = highAvailabilityOptions;
            this.secretProvider = secretProvider;
            this.licenseProvider = licenseProvider;
            this.dataProtectionOptions = dataProtectionOptions;
            this.certSyncProvider = certSyncProvider;
            this.dialogCoordinator = dialogCoordinator;
            this.rekeyProvider = rekeyProvider;

            this.licenseProvider.OnLicenseDataChanged += delegate
            {
                this.NotifyOfPropertyChange(nameof(this.IsEnterpriseEdition));
                this.NotifyOfPropertyChange(nameof(this.ShowEnterpriseEditionBanner));
            };

            this.DisplayName = "High availability";
            eventPublisher.Register(this);

            this.isClusterCompatibleSecretEncryptionEnabled = this.dataProtectionOptions.EnableClusterCompatibleSecretEncryption;
        }

        public string HelpLink => Constants.HelpLinkPageBitLocker;

        public PackIconMaterialKind Icon => PackIconMaterialKind.Server;

        public async Task Help()
        {
            await this.shellExecuteProvider.OpenWithShellExecute(this.HelpLink); // TODO Update link
        }

        public void LinkHaLearnMore()
        {
            // TODO Add link
        }

        public bool IsEnterpriseEdition => this.licenseManager.IsEnterpriseEdition();

        public bool ShowEnterpriseEditionBanner => this.licenseManager.IsEvaulatingOrBuiltIn() || !this.licenseManager.IsEnterpriseEdition();

        [NotifyModelChangedProperty]
        [AlsoNotifyFor(nameof(UseSqlServer))]
        public bool UseLocalDB
        {
            get => !this.highAvailabilityOptions.UseExternalSql;
            set => this.highAvailabilityOptions.UseExternalSql = !value;
        }

        [AlsoNotifyFor(nameof(UseLocalDB))]
        [NotifyModelChangedProperty]
        public bool UseSqlServer
        {
            get => this.highAvailabilityOptions.UseExternalSql;
            set => this.highAvailabilityOptions.UseExternalSql = value;
        }

        [NotifyModelChangedProperty]
        public string ConnectionString
        {
            get => this.highAvailabilityOptions.DbConnectionString;
            set => this.highAvailabilityOptions.DbConnectionString = value;
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

        public bool CanTestConnectionString => this.UseSqlServer && !string.IsNullOrWhiteSpace(this.ConnectionString);

        public void TestConnectionString()
        {

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
    }
}