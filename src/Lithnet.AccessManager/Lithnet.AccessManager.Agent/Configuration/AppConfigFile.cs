using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Lithnet.AccessManager.Agent.Configuration;

namespace Lithnet.AccessManager.Agent
{
    public class AppConfigFile
    {
        public AgentOptions Agent { get; set; } = new AgentOptions();

        [JsonExtensionData]
        public Dictionary<string, JsonElement> ExtensionData { get; set; }
    }
}
