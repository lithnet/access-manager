using Microsoft.Extensions.Logging;
using System;
using System.Data.SqlClient;

namespace Lithnet.AccessManager.Server.Providers
{
    public class SqlServerInstanceProvider : ISqlInstanceProvider
    {
        private readonly ILogger<SqlServerInstanceProvider> logger;
        private readonly IWindowsServiceProvider windowsServiceProvider;
        private readonly IRegistryProvider registryProvider;

        public SqlServerInstanceProvider(ILogger<SqlServerInstanceProvider> logger, IWindowsServiceProvider windowsServiceProvider, IRegistryProvider registryProvider)
        {
            this.logger = logger;
            this.windowsServiceProvider = windowsServiceProvider;
            this.registryProvider = registryProvider;
        }

        public string ConnectionString { get; private set; }

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

        public void InitializeDb()
        {
            this.logger.LogTrace("Initializing external DB");

            if (string.IsNullOrWhiteSpace(this.registryProvider.ConnectionString))
            {
                var builder = new SqlConnectionStringBuilder();

                builder.InitialCatalog = "AccessManager";
                builder.DataSource = registryProvider.SqlServer;
                builder.IntegratedSecurity = true;
                this.ConnectionString = builder.ToString();
            }
            else
            {
                this.ConnectionString = this.registryProvider.ConnectionString;
            }

            if (!this.DoesDbExist(this.ConnectionString))
            {
                this.logger.LogTrace($"Database does not exist. Attempting to create new DB.");
                this.CreateDatabase(this.ConnectionString);
            }
            else
            {
                this.logger.LogTrace($"Database exists");
            }

            using (this.GetConnection())
            {
                this.logger.LogTrace("Database connection successful");
            }
        }

        public string NormalizeConnectionString(string connectionString, string initialCatalog = "AccessManager")
        {
            if (!connectionString.Contains("="))
            {
                // The connection string doesn't contain any key-value pairs, so assume they provided only a server name;
                connectionString = $"Server={connectionString}";
            }

            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(connectionString);
            builder.InitialCatalog = initialCatalog;

            if (string.IsNullOrWhiteSpace(builder.UserID))
            {
                builder.IntegratedSecurity = true;
            }

            return builder.ToString();
        }

        public void TestConnectionString(string connectionString)
        {
            connectionString = this.NormalizeConnectionString(connectionString);

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
            }
        }

        public void CreateDatabase(string connectionString)
        {
            string masterDbConnectionString = this.NormalizeConnectionString(connectionString, "master");

            if (!this.DoesDbExist(masterDbConnectionString))
            {
                this.CreateDb(masterDbConnectionString);
            }
        }

        public bool DoesDbExist(string connectionString)
        {
            connectionString = this.NormalizeConnectionString(connectionString, "master");

            var sql = "IF DB_ID('AccessManager') IS NULL SELECT 0 ELSE SELECT 1";

            using (var con = new SqlConnection(connectionString))
            {
                con.Open();
                SqlCommand command = new SqlCommand(sql, con);
                return (int)command.ExecuteScalar() == 1;
            }
        }

        private void CreateDb(string masterDbConnectionString)
        {
            this.logger.LogTrace("The database was not found and will be created");

            using (var con = new SqlConnection(masterDbConnectionString))
            {
                con.Open();
                this.ExecuteNonQuery(EmbeddedResourceProvider.GetResourceString("CreateNewDatabase.sql", "DBScripts.ExternalSqlCreation"), con);
                this.ExecuteNonQuery(EmbeddedResourceProvider.GetResourceString("CreateServiceAccountLoginToServer.sql", "DBScripts.ExternalSqlCreation"), con);
                this.ExecuteNonQuery(EmbeddedResourceProvider.GetResourceString("CreateServiceAccountLoginToDB.sql", "DBScripts.ExternalSqlCreation"), con);
                this.ExecuteNonQuery(EmbeddedResourceProvider.GetResourceString("CreateServiceAccountPermissionToDB.sql", "DBScripts.ExternalSqlCreation"), con);
                this.ExecuteNonQuery(EmbeddedResourceProvider.GetResourceString("CreateAmsAdminsGroupLoginToDB.sql", "DBScripts.ExternalSqlCreation"), con);
                this.ExecuteNonQuery(EmbeddedResourceProvider.GetResourceString("CreateAmsAdminGroupPermissionToDB.sql", "DBScripts.ExternalSqlCreation"), con);
            }

            this.logger.LogTrace("The [AccessManager] database was created");
        }

        private void ExecuteNonQuery(string commandText, SqlConnection con)
        {
            commandText = commandText
                .Replace("{serviceAccount}", this.windowsServiceProvider.GetServiceNTAccount().Value, StringComparison.OrdinalIgnoreCase);

            string adminGroup = this.registryProvider.AmsAdminSid?.ToNtAccountName() ?? throw new InvalidOperationException("The AMS admin group was not set or not resolvable");
            commandText = commandText.Replace("{amsAdminsGroup}", adminGroup);

            SqlCommand command = new SqlCommand(commandText, con);
            this.logger.LogTrace("Executing command\r\n{sql}", command.CommandText);
            command.ExecuteNonQuery();
        }
    }
}
