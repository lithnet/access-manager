using System.ComponentModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Lithnet.AccessManager
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum GroupType : uint
    {
        [Description("Domain local")]
        DomainLocal = 0x80000004,

        [Description("Global")]
        Global = 0x80000002,

        [Description("Universal")]
        Universal = 0x80000008
    }
}
