using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Lithnet.AccessManager.Server.Configuration
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum AuthorizationServerType
    {
        Default = 0,
    }
}