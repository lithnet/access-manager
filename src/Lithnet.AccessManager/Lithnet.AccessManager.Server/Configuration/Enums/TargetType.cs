using System.ComponentModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Lithnet.AccessManager.Server.Configuration
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum TargetType
    {
        [Description("AD computer")]
        AdComputer = 0,

        [Description("AD group")]
        AdGroup = 1,

        [Description("AD container")]
        AdContainer = 2,

        [Description("Azure AD computer")]
        AadComputer = 3,

        [Description("Azure AD group")]
        AadGroup = 4,

        [Description("Azure AD tenant")]
        AadTenant = 7,
        
        [Description("AMS computer")]
        AmsComputer = 5,

        [Description("AMS group")]
        AmsGroup = 6,

        [Description("AD computer")]
        Computer = AdComputer,

        [Description("AD group")]
        Group = AdGroup,

        [Description("AD container")]
        Container = AdContainer
    }
}