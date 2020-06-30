using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Linq;
using Lithnet.AccessManager.Configuration;
using Microsoft.AspNetCore.Builder;
using Newtonsoft.Json;

namespace Lithnet.AccessManager.Server.Configuration
{
    public class ApplicationConfig : IApplicationConfig
    {
        public HostingOptions Hosting { get; set; }

        public AuthenticationOptions Authentication { get; set; }

        public AuditOptions Auditing { get; set; }

        public EmailOptions Email { get; set; }

        public RateLimitOptions RateLimits { get; set; }

        public UserInterfaceOptions UserInterface { get; set; }

        public ForwardedHeadersOptions ForwardedHeadersOptions { get; set; }

        public AuthorizationOptions Authorization { get; set; }

        public void Save(string file)
        {
            string data = JsonConvert.SerializeObject(this);
            File.WriteAllText(file, data);
        }

        public static IApplicationConfig Load(string file)
        {
            string data = File.ReadAllText(file);
            return JsonConvert.DeserializeObject<ApplicationConfig>(data);
        }
    }
}
