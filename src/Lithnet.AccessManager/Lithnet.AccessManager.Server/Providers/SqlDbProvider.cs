using DbUp;
using DbUp.Engine.Output;
using Lithnet.AccessManager.Server.Providers;
using Microsoft.Extensions.Logging;
using System.Data.SqlClient;
using System.Reflection;
using System.Threading;

namespace Lithnet.AccessManager.Server
{
    public class SqlDbProvider : IDbProvider
    {
        private const string MutexName = "{3D9036C2-9C6A-4166-BEC4-9564FDA11A45}";
        private readonly ILogger<SqlDbProvider> logger;
        private readonly IUpgradeLog upgradeLogger;
        private readonly SqlServerInstanceProvider sqlServerInstanceProvider;
        private static bool hasUpgraded = false;

        private ISqlInstanceProvider activeInstanceProvider;

        public SqlDbProvider(ILogger<SqlDbProvider> logger, IUpgradeLog upgradeLogger, SqlServerInstanceProvider sqlServerInstanceProvider)
        {
            this.logger = logger;
            this.upgradeLogger = upgradeLogger;
            this.sqlServerInstanceProvider = sqlServerInstanceProvider;
            this.InitializeDb();
        }

        public SqlConnection GetConnection()
        {
            return this.activeInstanceProvider.GetConnection();
        }

        public string ConnectionString => this.activeInstanceProvider.ConnectionString;

        private void InitializeDb()
        {
            this.activeInstanceProvider = this.sqlServerInstanceProvider;

            this.activeInstanceProvider.InitializeDb();

            if (hasUpgraded)
            {
                this.logger.LogTrace("Skipping upgrade because it has already been done");
                return;
            }

            var upgrader = DeployChanges.To
                .SqlDatabase(this.ConnectionString)
                .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly(), script => script.StartsWith("Lithnet.AccessManager.Server.DBScripts.Upgrade", System.StringComparison.OrdinalIgnoreCase))
                .LogScriptOutput()
                .LogTo(this.upgradeLogger)
                .Build();

            if (upgrader.IsUpgradeRequired())
            {
                this.logger.LogInformation(EventIDs.DbUpgradeRequired, "The database requires updates");
                using Mutex mutex = new Mutex(false, MutexName);

                try
                {
                    this.logger.LogTrace("Attempting to obtain upgrader mutex");
                    mutex.WaitOne();
                    this.logger.LogTrace("Got mutex");

                    var result = upgrader.PerformUpgrade();

                    if (!result.Successful)
                    {
                        throw result.Error;
                    }
                }
                finally
                {
                    mutex.ReleaseMutex();
                    this.logger.LogTrace("Released mutex");
                }

                hasUpgraded = true;
            }
            else
            {
                this.logger.LogTrace("The database is up to date");
            }
        }
    }
}
