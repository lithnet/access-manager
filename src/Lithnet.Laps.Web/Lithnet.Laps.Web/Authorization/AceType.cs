using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Runtime.Serialization;

namespace Lithnet.Laps.Web.Authorization
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum AceType
    {
        [EnumMember(Value = "not-defined")]
        NotDefined = 0,

        [EnumMember(Value = "allow")]
        Allow = 1,

        [EnumMember(Value = "deny")]
        Deny = 2,
    }
}