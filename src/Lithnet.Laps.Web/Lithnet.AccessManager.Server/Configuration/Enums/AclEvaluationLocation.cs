using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Lithnet.AccessManager.Server.Configuration
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum AclEvaluationLocation
    {
        ComputerDomain = 0,

        UserDomain = 1,

        WebAppDomain = 2,
    }
}