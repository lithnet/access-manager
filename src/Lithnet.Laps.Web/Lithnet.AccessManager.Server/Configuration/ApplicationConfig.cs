using System;
using System.Collections.Generic;
using System.Text;
using Lithnet.AccessManager.Configuration;

namespace Lithnet.AccessManager.Server.Configuration
{
    public class ApplicationConfig
    {
        public HostingOptions Hosting { get; set; }

        public AuthenticationOptions Authentication { get; set; }

        public AuditOptions Auditing { get; set; }

        public EmailOptions Email { get; set; }

        public RateLimitOptions RateLimits { get; set; }

        public UserInterfaceOptions UserInterface { get; set; }
    }
}
