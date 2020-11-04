using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lithnet.AccessManager.Server.Workers
{
    public class CertificateImportWorker : BackgroundService
    {
        private readonly ILogger logger;
        private readonly ICertificateSynchronizationProvider syncProvider;

        public CertificateImportWorker(ILogger<CertificateImportWorker> logger, ICertificateSynchronizationProvider syncProvider)
        {
            this.logger = logger;
            this.syncProvider = syncProvider;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            this.logger.LogTrace("Starting certificate synchronization background processing thread");
            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            this.logger.LogTrace("Stopping certificate synchronization background processing thread");
            return base.StopAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                this.syncProvider.ImportCertificatesFromConfig();
                await Task.Delay(-1, cancellationToken);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.CertificateSynchronizationImportError, ex, "The certificate import process failed");
            }
        }
    }
}
