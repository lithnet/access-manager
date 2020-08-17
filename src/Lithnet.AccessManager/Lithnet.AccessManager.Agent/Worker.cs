using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lithnet.AccessManager.Agent
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> logger;

        private readonly IAgentSettings settings;

        private readonly IHostApplicationLifetime appLifetime;

        private readonly IJitAgent jitAgent;

        private readonly ILapsAgent lapsAgent;

        private readonly ILocalSam sam;

        public Worker(ILogger<Worker> logger, IAgentSettings settings, IHostApplicationLifetime appLifetime, IJitAgent jitWorker, ILapsAgent lapsWorker, ILocalSam sam)
        {
            this.logger = logger;
            this.settings = settings;
            this.appLifetime = appLifetime;
            this.jitAgent = jitWorker;
            this.lapsAgent = lapsWorker;
            this.sam = sam;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation(EventIDs.AgentStarted, "Lithnet Access Manager Agent has started. v{version} {bits}", Assembly.GetEntryAssembly()?.GetName().Version, IntPtr.Size == 4 ? "(32-bit)" : "(64-bit)");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    logger.LogTrace("Worker running at: {time}", DateTimeOffset.Now);

                    if (this.sam.IsDomainController())
                    {
                        this.logger.LogWarning(EventIDs.RunningOnDC, "This application should not be run on a domain controller. Shutting down");
                        this.appLifetime.StopApplication();
                        return;
                    }

                    this.RunCheck();
                }
                catch (Exception ex)
                {
                    this.logger.LogError(EventIDs.AgentUnexpectedException, ex, "An unexpected error occurred");
                }

                await Task.Delay(TimeSpan.FromMinutes(Math.Max(this.settings.Interval, 5)), stoppingToken);
            }

        }

        private void RunCheck()
        {
            try
            {
                if (!this.settings.Enabled)
                {
                    logger.LogTrace(EventIDs.AgentDisabled, "Lithnet Access Manager agent is not enabled");
                    return;
                }

                try
                {
                    this.jitAgent.DoCheck();
                }
                catch (Exception ex)
                {
                    this.logger.LogError(EventIDs.JitUnexpectedException, ex, "The JIT worker encountered an exception");
                }

                try
                {
                    this.lapsAgent.DoCheck();
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