using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using System;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Server.Providers
{
    public class WebServiceHealthCheckProvider : IHealthCheck
    {
        private readonly IDbProvider sqlProvider;
        private readonly ILogger<WebServiceHealthCheckProvider> logger;

        public WebServiceHealthCheckProvider(IDbProvider dbProvider, ILogger<WebServiceHealthCheckProvider> logger)
        {
            this.sqlProvider = dbProvider;
            this.logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new CancellationToken())
        {
            try
            {
                await using var con = this.sqlProvider.GetConnection();
                SqlCommand command = new SqlCommand("SELECT 1", con);
                if ((int)(await command.ExecuteScalarAsync(cancellationToken)) != 1)
                {
                    return HealthCheckResult.Unhealthy("DB connection failed");
                }
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(ex, "Heath check failed");
                return HealthCheckResult.Unhealthy();
            }

            return HealthCheckResult.Healthy();
        }
    }
}
