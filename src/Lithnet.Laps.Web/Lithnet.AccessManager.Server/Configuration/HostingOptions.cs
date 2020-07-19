using System.Collections.Generic;
using System.IO;
using System.Xml.XPath;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace Lithnet.AccessManager.Server.Configuration
{
    public class HostingOptions
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public HostingEnvironment Environment { get; set; }

        public HttpSysHostingOptions HttpSys { get; set; } = new HttpSysHostingOptions();

        public void Save(string path)
        {
            var hostingOptions = new
            {
                Hosting = this
            };

            // var result = $"{{\r\n\t\"Hosting\": \r\n\t\t {JsonConvert.SerializeObject(this, Formatting.Indented)} \r\n}}";
            var result = JsonConvert.SerializeObject(hostingOptions, Formatting.Indented);

            File.WriteAllText(path, result);
        }

        public static HostingOptions Load(string file)
        {
            string data = File.ReadAllText(file);

            return JObject.Parse(data).SelectToken("Hosting")?.ToObject<HostingOptions>() ?? new HostingOptions();
        }
    }
}