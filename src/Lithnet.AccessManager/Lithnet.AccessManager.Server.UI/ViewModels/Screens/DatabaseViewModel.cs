using System;
using System.Data.SqlClient;
using Lithnet.AccessManager.Server.Configuration;
using Stylet;
using System.Threading.Tasks;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.IconPacks;
using Microsoft.Extensions.Logging;

namespace Lithnet.AccessManager.Server.UI
{
    public class DatabaseViewModel : Screen, IHelpLink
    {
        private readonly DatabaseOptions databaseOptions;
        private readonly IShellExecuteProvider shellExecuteProvider;
        private readonly IDbProvider dbProvider;
        private readonly ILogger<DatabaseViewModel> logger;
        private readonly IDialogCoordinator dialogCoordinator;
        private readonly IRegistryProvider registryProvider;

        public DatabaseViewModel(DatabaseOptions databaseOptions, INotifyModelChangedEventPublisher eventPublisher, IShellExecuteProvider shellExecuteProvider, IDbProvider dbProvider, ILogger<DatabaseViewModel> logger, IDialogCoordinator dialogCoordinator, IRegistryProvider registryProvider)
        {
            this.shellExecuteProvider = shellExecuteProvider;
            this.dbProvider = dbProvider;
            this.logger = logger;
            this.dialogCoordinator = dialogCoordinator;
            this.registryProvider = registryProvider;
            this.databaseOptions = databaseOptions;
            this.DisplayName = "Database";
            this.PopulateConnectionString();

            eventPublisher.Register(this);
        }

        public string HelpLink => Constants.HelpLinkDatabasePage;

        public PackIconMaterialKind Icon => PackIconMaterialKind.Database;

        [NotifyModelChangedProperty]
        public bool BackupCleanupEnabled { get => this.databaseOptions.BackupCleanupEnabled; set => this.databaseOptions.BackupCleanupEnabled = value; }

        [NotifyModelChangedProperty]
        public bool EnableBackup { get => this.databaseOptions.EnableBackup; set => this.databaseOptions.EnableBackup = value; }

        [NotifyModelChangedProperty]
        public bool EnableBuiltInMaintenance { get => this.databaseOptions.EnableBuiltInMaintenance; set => this.databaseOptions.EnableBuiltInMaintenance = value; }

        [NotifyModelChangedProperty]
        public string BackupPath { get => this.databaseOptions.BackupPath; set => this.databaseOptions.BackupPath = value; }

        [NotifyModelChangedProperty]
        public int BackupRetentionDays { get => this.databaseOptions.BackupRetentionDays; set => this.databaseOptions.BackupRetentionDays = value; }

        public string DatabaseServer { get; set; }

        public string DatabaseName { get; set; }

        private void PopulateConnectionString()
        {
            try
            {
                using var connection = dbProvider.GetConnection();
                this.DatabaseServer = connection.DataSource;
                this.DatabaseName = connection.Database;
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIGenericError, ex, "Unable to determine database information");
                this.DatabaseServer = "Unknown";
                this.DatabaseName = "Unknown";
            }
        }

        public async Task BackupNow()
        {
            ProgressDialogController progress = null;

            try
            {
                this.logger.LogInformation("Running database backup");

                progress = await this.dialogCoordinator.ShowProgressAsync(this, "Backup", "Preparing to start backup", false, new MetroDialogSettings { AnimateHide = false, AnimateShow = false });
                progress.SetIndeterminate();
                await Task.Delay(500);

                await using SqlConnection con = this.dbProvider.GetConnection();

                SqlCommand command = new SqlCommand("DatabaseBackup", con);
                command.CommandType = System.Data.CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@Databases", "AccessManager");
                command.Parameters.AddWithValue("@BackupType ", "FULL");

                if (this.BackupCleanupEnabled)
                {
                    command.Parameters.AddWithValue("@CleanupTime", this.BackupRetentionDays * 24);
                }

                if (!string.IsNullOrWhiteSpace(this.BackupPath))
                {
                    command.Parameters.AddWithValue("@Directory", this.BackupPath);
                }

                con.InfoMessage += delegate (object sender, SqlInfoMessageEventArgs e)
                {
                    this.logger.LogTrace(e.Message);
                };

                progress.SetMessage("Backup running...");
                await command.ExecuteNonQueryAsync();
                this.logger.LogInformation("Database backup completed");

                await progress.CloseAsync();

                await this.dialogCoordinator.ShowMessageAsync(this, "Backup", "Backup complete");
            }
            catch (Exception ex)
            {
                if (progress?.IsOpen ?? false)
                {
                    await progress.CloseAsync();
                }

                this.logger.LogError(EventIDs.UIGenericError, ex, "Backup failed");
                await dialogCoordinator.ShowMessageAsync(this, "Backup", $"The backup failed to complete\r\n{ex.Message}");
            }
            finally
            {
                if (progress?.IsOpen ?? false)
                {
                    await progress.CloseAsync();
                }
            }
        }

        public async Task Help()
        {
            await this.shellExecuteProvider.OpenWithShellExecute(this.HelpLink);
        }
    }
}
