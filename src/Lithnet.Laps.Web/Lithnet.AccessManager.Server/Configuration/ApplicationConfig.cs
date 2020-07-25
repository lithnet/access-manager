using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace Lithnet.AccessManager.Server.Configuration
{
    public class ApplicationConfig : IApplicationConfig
    {
        [JsonIgnore]
        public string Path { get; set; }
               
        public AuthenticationOptions Authentication { get; set; }
        
        public AuthorizationOptions Authorization { get; set; }

        public AuditOptions Auditing { get; set; }

        public EmailOptions Email { get; set; }

        public RateLimitOptions RateLimits { get; set; }

        public UserInterfaceOptions UserInterface { get; set; }

        public ForwardedHeadersAppOptions ForwardedHeaders { get; set; }
        
        public JitConfigurationOptions JitConfiguration { get; set; }

        [JsonExtensionData]
        public IDictionary<string, object> OtherData { get; set; }

        public void Save(string file)
        {
            JsonSerializerSettings settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
            };

            string data = JsonConvert.SerializeObject(this, settings);
            File.WriteAllText(file, data);
        }

        public static IApplicationConfig Load(string file)
        {
            string data = File.ReadAllText(file);
            var result = JsonConvert.DeserializeObject<ApplicationConfig>(data);
            result.Path = file;

            return result;
        }
    }
}
