using System.ComponentModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Lithnet.AccessManager.Server.Configuration
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum GroupType
    {
        [Description("Domain local")]
        DomainLocal = 0,

        [Description("Global")]
        Global = 1,

        [Description("Universal")]
        Universal = 2
    }
}
