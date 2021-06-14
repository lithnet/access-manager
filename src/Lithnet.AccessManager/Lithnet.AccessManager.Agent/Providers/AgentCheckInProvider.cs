using System;
using System.Net;
using Lithnet.AccessManager.Agent.Providers;
using Lithnet.AccessManager.Api.Shared;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Agent
{
    public class AgentCheckInProvider : IAgentCheckInProvider
    {
        private readonly IHttpClientFactory httpClientFactory;
        private readonly ISettingsProvider settingsProvider;

        public AgentCheckInProvider(IHttpClientFactory httpClientFactory, ISettingsProvider settingsProvider)
        {
            this.httpClientFactory = httpClientFactory;
            this.settingsProvider = settingsProvider;
        }

        public async Task CheckinIfRequired()
        {
            if (DateTime.UtcNow > this.settingsProvider.LastCheckIn.AddHours(Math.Max(2, this.settingsProvider.CheckInIntervalHours)))
            {
                await this.CheckIn();
                this.settingsProvider.LastCheckIn = DateTime.UtcNow;
            }
        }

        public async Task<AgentCheckIn> GenerateCheckInData()
        {
            return new AgentCheckIn
            {
                AgentVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0.0",
                DnsName = (await Dns.GetHostEntryAsync("LocalHost")).HostName,
                Hostname = Environment.MachineName,
                OperatingSystem = RuntimeInformation.OSDescription,
                OperationSystemVersion = Environment.OSVersion.Version.ToString()
            };
        }

        private async Task CheckIn()
        {
            AgentCheckIn data = await this.GenerateCheckInData();

            using var client = this.httpClientFactory.CreateClient(Constants.HttpClientAuthBearer);
            using var httpResponseMessage = await client.PostAsync($"agent/checkin", data.AsJsonStringContent());

            var responseString = await httpResponseMessage.Content.ReadAsStringAsync();
            httpResponseMessage.EnsureSuccessStatusCode(responseString);
        }
    }
}