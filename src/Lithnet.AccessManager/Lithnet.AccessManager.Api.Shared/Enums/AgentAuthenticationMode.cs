using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Lithnet.AccessManager.Api.Shared
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum AgentAuthenticationMode
    {
        None = 0,
        Iwa = 1,
        Aad = 2,
        Ams = 4,
    }
}
