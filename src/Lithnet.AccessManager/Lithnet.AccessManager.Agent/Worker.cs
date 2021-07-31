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
        private readonly IAgentSettings settings;
        private readonly ILapsAgent lapsAgent;

        public Worker(ILogger<Worker> logger, IAgentSettings settings,  ILapsAgent lapsWorker)
        {
            this.logger = logger;
            this.settings = settings;
            this.lapsAgent = lapsWorker;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            this.logger.LogInformation(EventIDs.AgentStarted, "Lithnet Access Manager Agent has started. v{version} {bits}", Assembly.GetEntryAssembly()?.GetName().Version, IntPtr.Size == 4 ? "(32-bit)" : "(64-bit)");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    this.logger.LogTrace("Worker running at: {time}", DateTimeOffset.Now);

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