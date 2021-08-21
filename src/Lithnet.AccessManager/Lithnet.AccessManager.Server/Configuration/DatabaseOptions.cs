namespace Lithnet.AccessManager.Server.Configuration
{
    public class DatabaseOptions
    {
        public bool EnableBuiltInMaintenance { get; set; } = true;

        public bool BackupCleanupEnabled { get; set; } = true;

        public int BackupRetentionDays { get; set; } = 14;

        public string BackupPath { get; set; }

        public bool EnableBackup { get; set; } = true;
    }
}
