using System;
using System.ServiceModel.Channels;
using System.Threading.Tasks;
using Lithnet.AccessManager.Enterprise;
using Lithnet.AccessManager.Server.Configuration;
using Lithnet.AccessManager.Server.Providers;
using Lithnet.AccessManager.Server.UI.Providers;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.IconPacks;
using Markdig.Extensions.TaskLists;
using Microsoft.Extensions.Logging;
using PropertyChanged;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class HighAvailabilityViewModel : Screen, IHelpLink
    {
        private readonly IDialogCoordinator dialogCoordinator;
        private readonly IShellExecuteProvider shellExecuteProvider;
        private readonly ILicenseManager licenseManager;
        private readonly ILogger<HighAvailabilityViewModel> logger;
        private readonly DatabaseConfigurationOptions dbOptions;
        private readonly DataProtectionOptions dataProtectionOptions;
        private readonly ICertificateSynchronizationProvider certSyncProvider;
        private readonly ISecretRekeyProvider rekeyProvider;
        private readonly SqlServerInstanceProvider sqlInstanceProvider;
        private readonly IScriptTemplateProvider scriptTemplateProvider;
        private readonly IWindowsServiceProvider windowsServiceProvider;

        public HighAvailabilityViewModel(IDialogCoordinator dialogCoordinator, IShellExecuteProvider shellExecuteProvider, ILicenseManager licenseManager, ILogger<HighAvailabilityViewModel> logger, INotifyModelChangedEventPublisher eventPublisher, DatabaseConfigurationOptions highAvailabilityOptions, DataProtectionOptions dataProtectionOptions, ICertificateSynchronizationProvider certSyncProvider, ISecretRekeyProvider rekeyProvider, SqlServerInstanceProvider sqlInstanceProvider, IScriptTemplateProvider scriptTemplateProvider, IWindowsServiceProvider windowsServiceProvider)
        {
            this.shellExecuteProvider = shellExecuteProvider;
            this.licenseManager = licenseManager;
            this.logger = logger;
            this.dbOptions = highAvailabilityOptions;
            this.dataProtectionOptions = dataProtectionOptions;
            this.certSyncProvider = certSyncProvider;
            this.dialogCoordinator = dialogCoordinator;
            this.rekeyProvider = rekeyProvider;
            this.sqlInstanceProvider = sqlInstanceProvider;
            this.scriptTemplateProvider = scriptTemplateProvider;
            this.windowsServiceProvider = windowsServiceProvider;

            this.licenseManager.OnLicenseDataChanged += delegate
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
            get => !this.dbOptions.UseExternalSql;
            set => this.dbOptions.UseExternalSql = !value;
        }

        [AlsoNotifyFor(nameof(UseLocalDB))]
        [NotifyModelChangedProperty]
        public bool UseSqlServer
        {
            get => this.dbOptions.UseExternalSql;
            set => this.dbOptions.UseExternalSql = value;
        }

        [NotifyModelChangedProperty]
        public string ConnectionString
        {
            get => this.dbOptions.ConnectionString;
            set => this.dbOptions.ConnectionString = value;
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

        public bool CanEditConnectionString => this.UseSqlServer;

        public async Task EditConnectionString()
        {
            ProgressDialogController progress = null;

            MetroDialogSettings settings = new MetroDialogSettings
            {
                DefaultText = this.ConnectionString,
            };

            var connectionString = await this.dialogCoordinator.ShowInputAsync(this, "Enter connection string", "Please provide the connection string to the database server that contains the AccessManager database", settings);

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return;
            }

            progress = await this.dialogCoordinator.ShowProgressAsync(this, "Testing connection", string.Empty, false);
            progress.SetIndeterminate();

            try
            {
                connectionString = sqlInstanceProvider.NormalizeConnectionString(connectionString);
                await Task.Run(() => sqlInstanceProvider.TestConnectionString(connectionString));
                this.ConnectionString = connectionString;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Could not change the connection string because the connection failed");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Could not connect to the database using the specified connection string\r\n\r\n{ex.Message}");
            }
            finally
            {
                if (progress != null)
                {
                    await progress.CloseAsync();
                }
            }
        }

        public bool CanCreateDatabase => this.IsEnterpriseEdition;

        public async Task CreateDatabase()
        {
            ProgressDialogController progress = null;

            try
            {
                MetroDialogSettings settings = new MetroDialogSettings
                {
                    DefaultText = this.ConnectionString,
                };

                var connectionString = await this.dialogCoordinator.ShowInputAsync(this, "Enter connection string", "Please provide the connection string to the database server where you want to create the database", settings);

                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    return;
                }

                progress = await this.dialogCoordinator.ShowProgressAsync(this, "Create new database", "Checking for existing database", false);
                progress.SetIndeterminate();
                connectionString = sqlInstanceProvider.NormalizeConnectionString(connectionString);

                bool exists = await Task.Run(() => sqlInstanceProvider.DoesDbExist(connectionString));

                if (exists)
                {
                    await this.dialogCoordinator.ShowMessageAsync(this, "Cannot create the database", "The database already exists");
                    return;
                }

                progress.SetMessage("Creating database");
                await Task.Run(() => sqlInstanceProvider.CreateDatabase(connectionString));

                this.ConnectionString = connectionString;
                await this.dialogCoordinator.ShowMessageAsync(this, "Operation successful", "The database was successfully created");
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "The database creation failed");
                await this.dialogCoordinator.ShowMessageAsync(this, "Cannot create the database", ex.Message);
            }
            finally
            {
                if (progress != null)
                {
                    await progress.CloseAsync();
                }
            }
        }

        public bool CanGetDatabaseCreationScript => this.IsEnterpriseEdition;

        public void GetDatabaseCreationScript()
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
        public bool CanTestConnectionString => this.UseSqlServer && !string.IsNullOrWhiteSpace(this.ConnectionString);

        public async Task TestConnectionString()
        {
            ProgressDialogController progress = null;

            try
            {
                progress = await this.dialogCoordinator.ShowProgressAsync(this, "Testing connection", string.Empty, false);
                progress.SetIndeterminate();

                await Task.Run(() => sqlInstanceProvider.TestConnectionString(this.ConnectionString));
                await this.dialogCoordinator.ShowMessageAsync(this, "Test successful", "The database test was successful");
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "The connection string test failed");
                await this.dialogCoordinator.ShowMessageAsync(this, "Test failed", ex.Message);
            }
            finally
            {
                if (progress != null)
                {
                    await progress.CloseAsync();
                }
            }
        }
    }
}
