using System.ComponentModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Lithnet.AccessManager.Server.Configuration
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum TargetType
    {
        [Description("AD-joined computer")]
        AdComputer = 0,

        [Description("AD group")]
        AdGroup = 1,

        [Description("AD container")]
        AdContainer = 2,

        [Description("Azure AD-managed computer")]
        AadComputer = 3,

        [Description("Azure AD group")]
        AadGroup = 4,

        [Description("AMS-managed computer")]
        AmsComputer = 5,

        [Description("AMS-managed computer group")]
        AmsGroup = 6,

        [Description("AD-joined computer")]
        Computer = AdComputer,

        [Description("AD group")]
        Group = AdGroup,

        [Description("AD container")]
        Container = AdContainer
    }
}