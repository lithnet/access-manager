using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lithnet.AccessManager.Api
{
    public class DisabledHost : IHostedService
    {
        private readonly ILogger<DisabledHost> logger;

        public DisabledHost(ILogger<DisabledHost> logger)
        {
            this.logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation(EventIDs.ApiNotEnabled, "The API service is not enabled");
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
