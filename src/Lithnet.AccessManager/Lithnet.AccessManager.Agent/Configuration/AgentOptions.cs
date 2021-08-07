using System.Text.Json.Serialization;

namespace Lithnet.AccessManager.Agent.Configuration
{
    public class AgentOptions
    {
        public int Interval { get; set; } = 60;

        public bool Enabled { get; set; } = true;

        public int CheckInIntervalHours { get; set; } = 24;

        [JsonIgnore]
        public bool AmsServerManagementEnabled => true;

        public bool EnableAdminAccount { get; set; } = true;

        public string Server { get; set; }
    }
}
