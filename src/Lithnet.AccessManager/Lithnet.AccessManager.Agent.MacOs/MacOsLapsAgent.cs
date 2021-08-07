using Lithnet.AccessManager.Agent.Providers;
using Lithnet.AccessManager.Api.Shared;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Agent
{
    public class MacOsLapsAgent : ILapsAgent
    {
        private readonly ILogger<MacOsLapsAgent> logger;
        private readonly IAgentSettings agentSettings;
        private readonly AmsLapsAgent amsLapsAgent;

        public MacOsLapsAgent(ILogger<MacOsLapsAgent> logger, AmsLapsAgent advancedLapsAgent, IAgentSettings agentSettings, IHostApplicationLifetime lifetime)
        {
            this.logger = logger;
            this.amsLapsAgent = advancedLapsAgent;
            this.agentSettings = agentSettings;
        }

        public async Task DoCheck()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(this.agentSettings.Server))
                {
                    var executable = Environment.GetCommandLineArgs()[0];
                    if (executable.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                    {
                        executable = executable.Substring(0, executable.Length - 4);
                    }

                    this.logger.LogError($"The agent has not been configured. Run '{executable} --setup' to configure the agent");
                    return;
                }

                if (!this.agentSettings.AmsPasswordStorageEnabled || !this.agentSettings.AmsServerManagementEnabled)
                {
                    this.logger.LogTrace("The password management functionality has been disabled in the configuration file");
                    return;
                }

                if (string.Equals(Environment.MachineName, "localhost", StringComparison.OrdinalIgnoreCase))
                {
                    this.logger.LogWarning("The agent cannot proceed because the computer hostname is 'localhost'. Give the computer a unique name and restart the agent");
                    return;
                }

                if (this.agentSettings.AuthenticationMode != AgentAuthenticationMode.Ams)
                {
                    this.logger.LogError($"The authentication mode {this.agentSettings.AuthenticationMode} specified in the configuration file is not supported on this operating system.");
                    return;
                }

                await this.amsLapsAgent.DoCheckAsync();
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.LapsUnexpectedException, ex, "The LAPS agent process encountered an error");
            }
        }
    }
}
