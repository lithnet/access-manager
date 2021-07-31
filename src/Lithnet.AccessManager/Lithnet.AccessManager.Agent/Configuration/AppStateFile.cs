using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Lithnet.AccessManager.Agent.Configuration;

namespace Lithnet.AccessManager.Agent
{
    public class AppStateFile
    {
        public AppState State { get; set; } = new AppState();

        [JsonExtensionData]
        public Dictionary<string, JsonElement> ExtensionData { get; set; }
    }
}
