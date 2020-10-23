using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lithnet.AccessManager.Service
{
    public class UnconfiguredHost : IHostedService
    {
        private readonly ILogger<UnconfiguredHost> logger;

        public UnconfiguredHost(ILogger<UnconfiguredHost> logger)
        {
            this.logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            logger.LogWarning(EventIDs.AppNotConfigured, "The service is started but is in a disabled state because the configuration has not yet been set");
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
