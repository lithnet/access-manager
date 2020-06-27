using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Lithnet.AccessManager.Web.Authorization
{
    [JsonConverter(typeof(StringEnumConverter))]
    [Flags]
    public enum AccessMask
    {
        [EnumMember(Value = "undefined")]
        Undefined = 0,

        [Description("Active local admin password")]
        [EnumMember(Value = "laps")]
        Laps = 1,

        [Description("Just-in-time access")]
        [EnumMember(Value = "jit")]
        Jit = 2,

        [Description("Previous local admin passwords")]
        [EnumMember(Value = "lapshistory")]
        LapsHistory = 4,
    }
}