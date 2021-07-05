using System.Data.SqlClient;
using System.Reflection;
using System.Threading;
using DbUp;
using DbUp.Engine.Output;
using Lithnet.AccessManager.Enterprise;
using Lithnet.AccessManager.Server.Configuration;
using Lithnet.AccessManager.Server.Providers;
using Lithnet.Licensing.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lithnet.AccessManager.Server
{
    public class SqlDbProvider : IDbProvider
    {
        private const string MutexName = "{3D9036C2-9C6A-4166-BEC4-9564FDA11A45}";
        private readonly IAmsLicenseManager licenseManager;
        private readonly DatabaseConfigurationOptions highAvailabilityOptions;
        private readonly ILogger<SqlDbProvider> logger;
        private readonly IUpgradeLog upgradeLogger;
        private readonly SqlLocalDbInstanceProvider localDbInstanceProvider;
        private readonly SqlServerInstanceProvider sqlServerInstanceProvider;
        private static bool hasUpgraded = false;

        private ISqlInstanceProvider activeInstanceProvider;

        public SqlDbProvider(IAmsLicenseManager licenseManager, IOptions<DatabaseConfigurationOptions> highAvailabilityOptions, ILogger<SqlDbProvider> logger, IUpgradeLog upgradeLogger, SqlLocalDbInstanceProvider localDbInstanceProvider, SqlServerInstanceProvider sqlServerInstanceProvider)
        {
            this.licenseManager = licenseManager;
            this.highAvailabilityOptions = highAvailabilityOptions.Value;
            this.logger = logger;
            this.upgradeLogger = upgradeLogger;
            this.localDbInstanceProvider = localDbInstanceProvider;
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
            if (licenseManager.IsFeatureEnabled(LicensedFeatures.ExternalSql) && highAvailabilityOptions.UseExternalSql)
            {
                this.activeInstanceProvider = this.sqlServerInstanceProvider;
            }
            else
            {
                this.activeInstanceProvider = this.localDbInstanceProvider;
            }

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
