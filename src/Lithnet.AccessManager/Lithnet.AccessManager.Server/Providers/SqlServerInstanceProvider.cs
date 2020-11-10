using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;
using Lithnet.AccessManager.Enterprise;
using Lithnet.AccessManager.Server.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lithnet.AccessManager.Server.Providers
{
    public class SqlServerInstanceProvider : ISqlInstanceProvider
    {
        private readonly ILogger<SqlServerInstanceProvider> logger;
        private readonly HighAvailabilityOptions highAvailabilityOptions;
        private readonly ILicenseManager licenseManager;

        public SqlServerInstanceProvider(ILogger<SqlServerInstanceProvider> logger, IOptions<HighAvailabilityOptions> highAvailabilityOptions, ILicenseManager licenseManager)
        {
            this.logger = logger;
            this.highAvailabilityOptions = highAvailabilityOptions.Value;
            this.licenseManager = licenseManager;
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
            this.licenseManager.ThrowOnMissingFeature(LicensedFeatures.ExternalSql);

            this.logger.LogTrace("Initializing external DB");
            this.ConnectionString = this.highAvailabilityOptions.DbConnectionString;
            
            using (this.GetConnection())
            {
                this.logger.LogTrace("Database connection successful");
            }
        }
    }
}
