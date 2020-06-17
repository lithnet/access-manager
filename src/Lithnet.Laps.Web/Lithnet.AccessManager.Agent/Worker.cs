using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lithnet.AccessManager.Agent
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> logger;

        private readonly IDirectory directory;

        private readonly ISettingsProvider settings;

        private readonly IHostApplicationLifetime appLifetime;

        private readonly IJitWorker jitWorker;

        public Worker(ILogger<Worker> logger, IDirectory directory, ISettingsProvider settings, IHostApplicationLifetime appLifetime, IJitWorker jitWorker)
        {
            this.logger = logger;
            this.directory = directory;
            this.settings = settings;
            this.appLifetime = appLifetime;
            this.jitWorker = jitWorker;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                logger.LogTrace("Worker running at: {time}", DateTimeOffset.Now);

                if (this.directory.IsDomainController())
                {
                    this.logger.LogWarning("This application should not be run on a domain controller. Shutting down");
                    this.appLifetime.StopApplication();
                    return;
                }
                
                this.RunCheck();

                await Task.Delay(TimeSpan.FromMinutes(Math.Max(this.settings.CheckInterval, 5)), stoppingToken);
            }
        }

        private void RunCheck()
        {
            try
            {
                if (!this.settings.Enabled)
                {
                    logger.LogTrace("Lithnet Access Manager agent is not enabled");
                    return;
                }

                try
                {
                    this.jitWorker.DoCheck();
                }
                catch(Exception ex)
                {
                    this.logger.LogError(ex, "The JIT worker encountered an exception");
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "An unexpected error occurred");
            }
        }
    }
}