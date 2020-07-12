using Lithnet.AccessManager.Server.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Lithnet.AccessManager.Server.Configuration
{
    public class HostingOptions
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public HostingEnvironment Environment { get; set; }

        public HttpSysHostingOptions HttpSys { get; set; }
    }
}