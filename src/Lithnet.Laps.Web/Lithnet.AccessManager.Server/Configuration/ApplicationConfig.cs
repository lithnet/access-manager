using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Xml.Linq;
using System.Xml.XPath;
using Lithnet.AccessManager.Configuration;
using Microsoft.AspNetCore.Builder;
using Newtonsoft.Json;

namespace Lithnet.AccessManager.Server.Configuration
{
    public class ApplicationConfig : IApplicationConfig
    {
        [JsonIgnore]
        public string Path { get; set; }

        public HostingOptions Hosting { get; set; }
                
        public AuthenticationOptions Authentication { get; set; }
        
        public AuthorizationOptions Authorization { get; set; }

        public AuditOptions Auditing { get; set; }

        public EmailOptions Email { get; set; }

        public RateLimitOptions RateLimits { get; set; }

        public UserInterfaceOptions UserInterface { get; set; }

        public ForwardedHeadersAppOptions ForwardedHeaders { get; set; }
        
        public JitConfigurationOptions JitConfiguration { get; set; }

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
