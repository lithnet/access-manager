using System.ComponentModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Lithnet.AccessManager.Server.Configuration
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum RoleTargetType
    {
        [Description("AD group")]
        AdGroup = 0,

        [Description("PowerShell scripted role")]
        ScriptedRole = 1,
    }
}