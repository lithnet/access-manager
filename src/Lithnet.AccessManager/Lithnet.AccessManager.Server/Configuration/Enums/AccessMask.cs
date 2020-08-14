using System;
using System.ComponentModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Lithnet.AccessManager.Server.Configuration
{
    [JsonConverter(typeof(StringEnumConverter))]
    [Flags]
    public enum AccessMask
    {
        None = 0,

        [Description("Local admin password")]
        LocalAdminPassword = 0x200,

        [Description("Local admin password history")]
        LocalAdminPasswordHistory = 0x400,

        [Description("Just-in-time access")]
        Jit = 0x800,
    }
}
