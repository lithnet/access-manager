using Lithnet.AccessManager.Api.Shared;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Agent.Providers
{
    public class AgentCheckInProvider : IAgentCheckInProvider
    {
        private readonly IAmsApiHttpClient httpClient;
        private readonly IAgentSettings settingsProvider;
        private readonly IPlatformDataProvider dataProvider;

        public AgentCheckInProvider(IAmsApiHttpClient httpClient, IAgentSettings settingsProvider, IPlatformDataProvider dataProvider)
        {
            this.httpClient = httpClient;
            this.settingsProvider = settingsProvider;
            this.dataProvider = dataProvider;
        }

        public async Task CheckinIfRequired()
        {
            AgentCheckIn data = await this.GenerateCheckInData();
            var hash = data.ToHash();

            if (hash != this.settingsProvider.CheckInDataHash ||
                DateTime.UtcNow > this.settingsProvider.LastCheckIn.AddHours(Math.Max(2, this.settingsProvider.CheckInIntervalHours)))
            {
                await this.CheckIn(data);
                this.settingsProvider.LastCheckIn = DateTime.UtcNow;
                this.settingsProvider.CheckInDataHash = hash;
            }
        }

        public Task<AgentCheckIn> GenerateCheckInData()
        {
            string machineName = this.dataProvider.GetMachineName();
            if (string.Equals(machineName, "localhost", StringComparison.OrdinalIgnoreCase))
            {
                throw new UnsupportedConfigurationException("The local machine has a name of 'localhost'. Rename the computer in order to continue");
            }

            return Task.FromResult(new AgentCheckIn
            {
                AgentVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0.0",
                DnsName = this.dataProvider.GetDnsName(),
                Hostname = machineName,
                OperatingSystem = this.dataProvider.GetOSName(),
                OperationSystemVersion = this.dataProvider.GetOSVersion()
            });
        }

        private async Task CheckIn(AgentCheckIn data)
        {
            await this.httpClient.CheckInAsync(data);
        }
    }
}