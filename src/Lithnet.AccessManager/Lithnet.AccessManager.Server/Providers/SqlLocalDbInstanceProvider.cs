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

        private readonly string localDbPath;
        private readonly string localDbLogPath;

        private SqlConnection holdConnection;
        private SqlLocalDbApi localDbApi;
        private ISqlLocalDbInstanceInfo localDbInstance;
        private ISqlLocalDbInstanceManager instanceManager;

        private string masterDbConnectionString;

        public SqlLocalDbInstanceProvider(IAppPathProvider appPathProvider, ILogger<SqlLocalDbInstanceProvider> logger, IHostApplicationLifetime appLifeTime)
        {
            this.appPathProvider = appPathProvider;
            this.logger = logger;
            this.appLifeTime = appLifeTime;

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
            this.StartLocalDbInstance();
            this.holdConnection = this.GetConnection();
        }

        private void StartLocalDbInstance()
        {
            this.localDbApi = new SqlLocalDbApi();
            this.logger.LogTrace($"Creating internal DB instance {dbInstanceName}");
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
                sql = $@"CREATE DATABASE [AccessManager]   
    ON (FILENAME = N'{this.localDbPath}'),   
    (FILENAME = N'{this.localDbLogPath}')   
    FOR ATTACH;";
            }
            else
            {
                sql = $@"CREATE DATABASE [AccessManager]
    ON (FILENAME = N'{this.localDbPath}')
    FOR ATTACH;";
            }

            using (var con = new SqlConnection(masterDbConnectionString))
            {
                con.Open();
                SqlCommand command = new SqlCommand(sql, con);
                command.ExecuteNonQuery();
            }
        }

        private void CreateDb()
        {
            this.logger.LogInformation(EventIDs.DbNotFound, "The data file {databaseFile} was not found and will be created", this.localDbPath);

            Directory.CreateDirectory(this.appPathProvider.DbPath);

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