using Lithnet.AccessManager.Agent.Providers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Agent
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> logger;
        private readonly ISettingsProvider settings;
        private readonly IHostApplicationLifetime appLifetime;
        private readonly ILapsAgent lapsAgent;
        private readonly ILocalSam sam;
        private readonly IPasswordStorageProvider passwordStorage;

        public Worker(ILogger<Worker> logger, ISettingsProvider settings, IHostApplicationLifetime appLifetime, ILapsAgent lapsWorker, ILocalSam sam, IPasswordStorageProvider passwordStorage)
        {
            this.logger = logger;
            this.settings = settings;
            this.appLifetime = appLifetime;
            this.lapsAgent = lapsWorker;
            this.sam = sam;
            this.passwordStorage = passwordStorage;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            this.logger.LogInformation(EventIDs.AgentStarted, "Lithnet Access Manager Agent has started. v{version} {bits}", Assembly.GetEntryAssembly()?.GetName().Version, IntPtr.Size == 4 ? "(32-bit)" : "(64-bit)");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    this.logger.LogTrace("Worker running at: {time}", DateTimeOffset.Now);

                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        if (this.sam.IsDomainController())
                        {
                            this.logger.LogWarning(EventIDs.RunningOnDC, "This application should not be run on a domain controller. Shutting down");
                            this.appLifetime.StopApplication();
                            return;
                        }
                    }

                    await this.RunCheck();
                }
                catch (Exception ex)
                {
                    this.logger.LogError(EventIDs.AgentUnexpectedException, ex, "An unexpected error occurred");
                }

                await Task.Delay(TimeSpan.FromMinutes(Math.Max(this.settings.Interval, 5)), stoppingToken);
            }

        }

        private async Task RunCheck()
        {
            try
            {
                if (!this.settings.Enabled)
                {
                    this.logger.LogTrace(EventIDs.AgentDisabled, "Lithnet Access Manager agent is not enabled");
                    return;
                }

                try
                {
                    await this.lapsAgent.DoCheck();
                }
                catch (Exception ex)
                {
                    this.logger.LogError(EventIDs.LapsUnexpectedException, ex, "The local admin password worker encountered an exception");
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.AgentUnexpectedException, ex, "An unexpected error occurred");
            }
        }
    }
}