using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace Lithnet.Laps.Web.Authorization
{
    [JsonConverter(typeof(StringEnumConverter))]
    [Flags]
    public enum AccessMask
    {
        [EnumMember(Value = "undefined")]
        Undefined = 0,

        [Description("Local admin password")]
        [EnumMember(Value = "laps")]
        Laps = 1,

        [Description("Just-in-time access")]
        [EnumMember(Value = "jit")]
        Jit = 2,
    }
}