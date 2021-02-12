using System;
using System.Data.SqlClient;
using System.IO;
using MartinCostello.SqlLocalDb;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lithnet.AccessManager.Server
{
    public class SqlLocalDbInstanceProvider : ISqlInstanceProvider
    {
        private const string dbFileNamePrefix = "ams";
        private const string dbInstanceName = "ams";

        private readonly IAppPathProvider appPathProvider;
        private readonly ILogger<SqlLocalDbInstanceProvider> logger;
        private readonly IHostApplicationLifetime appLifeTime;
        private readonly IRegistryProvider registryProvider;

        private readonly string localDbPath;
        private readonly string localDbLogPath;

        private SqlConnection holdConnection;
        private SqlLocalDbApi localDbApi;
        private ISqlLocalDbInstanceInfo localDbInstance;
        private ISqlLocalDbInstanceManager instanceManager;

        private string masterDbConnectionString;

        public SqlLocalDbInstanceProvider(IAppPathProvider appPathProvider, ILogger<SqlLocalDbInstanceProvider> logger, IHostApplicationLifetime appLifeTime, IRegistryProvider registryProvider)
        {
            this.appPathProvider = appPathProvider;
            this.logger = logger;
            this.appLifeTime = appLifeTime;

            this.registryProvider = registryProvider;
            this.localDbApi = new SqlLocalDbApi();
            this.localDbPath = Path.Combine(this.appPathProvider.DbPath, $"{dbFileNamePrefix}.mdf");
            this.localDbLogPath = Path.Combine(this.appPathProvider.DbPath, $"{dbFileNamePrefix}_log.ldf");

            this.appLifeTime.ApplicationStopped.Register(StopLocalDbInstance);
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
            this.logger.LogTrace("Initializing internal DB");
            this.DeleteLocalDbInstanceIfFlagged();
            this.StartLocalDbInstance();
            this.holdConnection = this.GetConnection();
        }

        private void DeleteLocalDbInstanceIfFlagged()
        {
            if (this.registryProvider.DeleteLocalDbInstance)
            {
                if (this.localDbApi.InstanceExists(dbInstanceName))
                {
                    this.logger.LogWarning(EventIDs.DbDeleting, "Deleting SqlLocalDB instance");

                    if (this.localDbApi.GetInstanceInfo(dbInstanceName).IsRunning)
                    {
                        this.localDbApi.StopInstance(dbInstanceName);
                    }

                    this.localDbApi.DeleteInstance(dbInstanceName);

                    this.logger.LogWarning(EventIDs.DbDeleted, "Deleted SqlLocalDB instance");
                }

                this.registryProvider.DeleteLocalDbInstance = false;
            }
        }

        private void StartLocalDbInstance()
        {
            bool creating = !this.localDbApi.InstanceExists(dbInstanceName);

            this.logger.LogTrace($"{(creating ? "Creating" : "Connecting to")} internal DB instance {dbInstanceName}");

            this.localDbInstance = this.localDbApi.CreateInstance(dbInstanceName);
            this.instanceManager = this.localDbInstance.Manage();

            if (!this.localDbInstance.IsRunning)
            {
                this.logger.LogTrace($"Starting internal DB instance {dbInstanceName}");
                this.instanceManager.Start();
            }

            var bbuilder = this.localDbInstance.CreateConnectionStringBuilder();
            bbuilder.InitialCatalog = "master";
            bbuilder.ConnectTimeout = 30;
            bbuilder.IntegratedSecurity = true;
            this.masterDbConnectionString = bbuilder.ToString();

            this.logger.LogTrace($"Master DB connection string {this.masterDbConnectionString}");

            if (creating)
            {
                this.EnableContainment();
            }

            if (!this.IsDbAttached())
            {
                if (File.Exists(this.localDbPath))
                {
                    this.logger.LogTrace($"Attaching existing DB {this.localDbPath} to instance");
                    this.AttachExistingDb();
                }
                else
                {
                    this.logger.LogTrace($"Creating new DB {this.localDbPath} in instance");
                    this.CreateDb();
                }
            }
            else
            {
                this.logger.LogTrace($"Database was already attached to instance {dbInstanceName}");
            }

            bbuilder = this.localDbInstance.CreateConnectionStringBuilder();
            bbuilder.InitialCatalog = "AccessManager";
            bbuilder.ConnectTimeout = 30;
            bbuilder.IntegratedSecurity = true;
            this.ConnectionString = bbuilder.ToString();

            this.logger.LogTrace($"AccessManager DB connection string {this.ConnectionString}");
        }

        private void StopLocalDbInstance()
        {
            if (this.localDbInstance?.IsRunning ?? false)
            {
                this.logger.LogTrace($"Stopping internal DB instance {dbInstanceName}");

                if (this.holdConnection?.State == System.Data.ConnectionState.Open)
                {
                    this.holdConnection.Close();
                }

                this.localDbApi.StopInstance(dbInstanceName, StopInstanceOptions.NoWait, TimeSpan.FromSeconds(10));
            }
        }

        private bool IsDbAttached()
        {
            var sql = "IF DB_ID('AccessManager') IS NULL SELECT 0 ELSE SELECT 1";

            using (var con = new SqlConnection(masterDbConnectionString))
            {
                con.Open();
                SqlCommand command = new SqlCommand(sql, con);
                return (int)command.ExecuteScalar() == 1;
            }
        }

        private void AttachExistingDb()
        {
            string sql;
            if (File.Exists(this.localDbLogPath))
            {
                sql = EmbeddedResourceProvider.GetResourceString("AttachDatabaseWithLog.sql", "DBScripts.LocalDBCreation");
            }
            else
            {
                sql = EmbeddedResourceProvider.GetResourceString("AttachDatabase.sql", "DBScripts.LocalDBCreation");
            }

            using (var con = new SqlConnection(masterDbConnectionString))
            {
                con.Open();
                this.ExecuteNonQuery(sql, con);
            }
        }

        private void EnableContainment()
        {
            this.logger.LogTrace("Enabling database containment");

            using (var con = new SqlConnection(masterDbConnectionString))
            {
                con.Open();
                this.ExecuteNonQuery(EmbeddedResourceProvider.GetResourceString("EnableContainment.sql", "DBScripts.LocalDBCreation"), con);
            }
        }

        private void CreateDb()
        {
            this.logger.LogInformation(EventIDs.DbNotFound, "The data file {databaseFile} was not found and will be created", this.localDbPath);

            Directory.CreateDirectory(this.appPathProvider.DbPath);


            using (var con = new SqlConnection(masterDbConnectionString))
            {
                con.Open();
                this.ExecuteNonQuery(EmbeddedResourceProvider.GetResourceString("CreateNewDatabaseWithPaths.sql", "DBScripts.LocalDBCreation"), con);
                this.ExecuteNonQuery(EmbeddedResourceProvider.GetResourceString("CreateServiceAccountLoginToServer.sql", "DBScripts.LocalDBCreation"), con);
                this.ExecuteNonQuery(EmbeddedResourceProvider.GetResourceString("CreateServiceAccountLoginToDB.sql", "DBScripts.LocalDBCreation"), con);
                this.ExecuteNonQuery(EmbeddedResourceProvider.GetResourceString("CreateServiceAccountPermissionToDB.sql", "DBScripts.LocalDBCreation"), con);
            }

            this.logger.LogInformation(EventIDs.DbCreated, "The [AccessManager] database was created");
        }

        private void ExecuteNonQuery(string commandText, SqlConnection con)
        {
            commandText = commandText
                .Replace("{localDbPath}", this.localDbPath, StringComparison.OrdinalIgnoreCase)
                .Replace("{localDbLogPath}", this.localDbLogPath, StringComparison.OrdinalIgnoreCase);

            SqlCommand command = new SqlCommand(commandText, con);
            this.logger.LogTrace("Executing command\r\n{sql}", command.CommandText);
            command.ExecuteNonQuery();
        }
    }
}