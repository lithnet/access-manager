using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Lithnet.AccessManager.Agent.Shared
{
    public class AgentJsonSettings
    {
        public static JsonSerializerOptions JsonSerializerDefaults = new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true,
            IgnoreNullValues = true,
            PropertyNamingPolicy = null,
            WriteIndented = true,
            AllowTrailingCommas = true
        };

    }
}
