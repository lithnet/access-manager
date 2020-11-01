using System.Data.SqlClient;
using System.IO;
using System.Reflection;
using DbUp;
using DbUp.Engine.Output;
using Lithnet.AccessManager.Enterprise;
using Lithnet.AccessManager.Server.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lithnet.AccessManager.Server
{
    public class SqlDbProvider : IDbProvider
    {
        private const string masterDbConnectionString = "Server=(LocalDB)\\MSSQLLocalDB;Integrated Security=true;Database=master;Connection Timeout=60;";
        private const string defaultConnectionString = "Server=(LocalDB)\\MSSQLLocalDB;Integrated Security=true;Database=AccessManager;Connection Timeout=60;";
        private const string dbFileNamePrefix = "ams";

        private readonly ILicenseManager licenseManager;
        private readonly HighAvailabilityOptions highAvailabilityOptions;
        private readonly IAppPathProvider appPathProvider;
        private readonly ILogger<SqlDbProvider> logger;
        private readonly IUpgradeLog upgradeLogger;

        private readonly string localDbPath;
        private readonly string localDbLogPath;

        private SqlConnection holdConnection;

        public SqlDbProvider(ILicenseManager licenseManager, IOptions<HighAvailabilityOptions> highAvailabilityOptions, IAppPathProvider appPathProvider, ILogger<SqlDbProvider> logger, IUpgradeLog upgradeLogger)
        {
            this.licenseManager = licenseManager;
            this.highAvailabilityOptions = highAvailabilityOptions.Value;
            this.appPathProvider = appPathProvider;
            this.logger = logger;
            this.upgradeLogger = upgradeLogger;

            this.localDbPath = Path.Combine(this.appPathProvider.DbPath, $"{dbFileNamePrefix}.mdf");
            this.localDbLogPath = Path.Combine(this.appPathProvider.DbPath, $"{dbFileNamePrefix}_log.ldf");
        }

        public SqlConnection GetConnection()
        {
            if (this.ConnectionString == null)
            {
                throw new DatabaseNotInitializedException("The database connection must first be initialized with a call to InitializeDb()");
            }

            var con = new SqlConnection(this.ConnectionString);
            con.Open();
            return con;
        }

        public string ConnectionString { get; private set; }

        public void InitializeDb()
        {
            if (licenseManager.IsFeatureEnabled(LicensedFeatures.ExternalSql) && highAvailabilityOptions.UseExternalSql)
            {
                this.logger.LogTrace("Attempting to connect to external sql server");
                this.ConnectionString = highAvailabilityOptions.DbConnectionString;
                EnsureDatabase.For.SqlDatabase(this.ConnectionString);
            }
            else
            {
                this.logger.LogTrace("Using internal db");
                this.ConnectionString = $"{defaultConnectionString};AttachDbFileName={this.localDbPath};";
                this.CreateLocalDbIfDoesntExist();
                this.holdConnection = this.GetConnection();
            }

            var upgrader = DeployChanges.To
            .SqlDatabase(this.ConnectionString)
            .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
            .LogScriptOutput()
            .LogTo(this.upgradeLogger)
            .Build();

            if (upgrader.IsUpgradeRequired())
            {
                this.logger.LogInformation(EventIDs.DbUpgradeRequired, "The database requires updates");
            }
            else
            {
                this.logger.LogTrace("The database is up to date");
            }

            // TODO: EventIDs

            var result = upgrader.PerformUpgrade();

            if (!result.Successful)
            {
                throw result.Error;
            }
        }

        private void CreateLocalDbIfDoesntExist()
        {
            if (File.Exists(this.localDbPath))
            {
                return;
            }

            this.logger.LogInformation(EventIDs.DbNotFound, "The data file {databaseFile} was not found and will be created", this.localDbPath);

            string createDbString = $@"CREATE DATABASE [AccessManager]
 CONTAINMENT = NONE
 ON  PRIMARY 
( NAME = N'AccessManager', FILENAME = N'{this.localDbPath}' , SIZE = 65536KB , MAXSIZE = UNLIMITED, FILEGROWTH = 65536KB )
 LOG ON 
( NAME = N'AccessManager_log', FILENAME = N'{this.localDbLogPath}' , SIZE = 65536KB, MAXSIZE = 2048GB , FILEGROWTH = 65536KB )
 WITH CATALOG_COLLATION = DATABASE_DEFAULT";

            using (var con = new SqlConnection(masterDbConnectionString))
            {
                con.Open();
                SqlCommand command = new SqlCommand(createDbString, con);
                command.ExecuteNonQuery();

                command = new SqlCommand(@"
USE [master]

IF NOT EXISTS 
    (SELECT name  
     FROM master.sys.server_principals
     WHERE name = 'NT SERVICE\lithnetams')
BEGIN
    CREATE LOGIN [NT SERVICE\lithnetams] FROM WINDOWS WITH DEFAULT_DATABASE=[AccessManager]
END
", con);
                command.ExecuteNonQuery();

                command = new SqlCommand(@"
USE [AccessManager]

IF NOT EXISTS
    (SELECT name
     FROM sys.database_principals
     WHERE name = 'NT SERVICE\lithnetams')
BEGIN
    CREATE USER [NT SERVICE\lithnetams] FOR LOGIN [NT SERVICE\lithnetams]
END
", con);
                command.ExecuteNonQuery();

                command = new SqlCommand(@"
USE [AccessManager]
ALTER ROLE [db_owner] ADD MEMBER [NT SERVICE\lithnetams]
", con);
                command.ExecuteNonQuery();
            }

            this.logger.LogInformation(EventIDs.DbCreated, "The [AccessManager] database was created");
        }
    }
}
