namespace Lithnet.AccessManager.Server.Configuration
{
    public class HighAvailabilityOptions
    {
        public string DbConnectionString { get; set; }

        public bool UseExternalSql { get; set; }
    }
}