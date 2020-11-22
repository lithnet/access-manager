using System.Data.SqlClient;
using System.Reflection;
using DbUp;
using DbUp.Engine.Output;
using Lithnet.AccessManager.Enterprise;
using Lithnet.AccessManager.Server.Configuration;
using Lithnet.AccessManager.Server.Providers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lithnet.AccessManager.Server
{
    public class SqlDbProvider : IDbProvider
    {
        private readonly ILicenseManager licenseManager;
        private readonly DatabaseConfigurationOptions highAvailabilityOptions;
        private readonly ILogger<SqlDbProvider> logger;
        private readonly IUpgradeLog upgradeLogger;
        private readonly SqlLocalDbInstanceProvider localDbInstanceProvider;
        private readonly SqlServerInstanceProvider sqlServerInstanceProvider;

        private ISqlInstanceProvider activeInstanceProvider;

        public SqlDbProvider(ILicenseManager licenseManager, IOptions<DatabaseConfigurationOptions> highAvailabilityOptions, ILogger<SqlDbProvider> logger, IUpgradeLog upgradeLogger, SqlLocalDbInstanceProvider localDbInstanceProvider, SqlServerInstanceProvider sqlServerInstanceProvider)
        {
            this.licenseManager = licenseManager;
            this.highAvailabilityOptions = highAvailabilityOptions.Value;
            this.logger = logger;
            this.upgradeLogger = upgradeLogger;
            this.localDbInstanceProvider = localDbInstanceProvider;
            this.sqlServerInstanceProvider = sqlServerInstanceProvider;
        }

        public SqlConnection GetConnection()
        {
            return this.activeInstanceProvider.GetConnection();
        }

        public string ConnectionString => this.activeInstanceProvider.ConnectionString;

        public void InitializeDb()
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

            var upgrader = DeployChanges.To
            .SqlDatabase(this.ConnectionString)
            .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly(), script => script.StartsWith("Lithnet.AccessManager.Server.DBScripts.Upgrade", System.StringComparison.OrdinalIgnoreCase))
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

            var result = upgrader.PerformUpgrade();

            if (!result.Successful)
            {
                throw result.Error;
            }
        }
    }
}
