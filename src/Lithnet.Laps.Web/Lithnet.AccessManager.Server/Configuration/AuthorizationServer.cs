using System;
using System.Collections.Generic;
using System.Text;

namespace Lithnet.AccessManager.Server.Configuration
{
    public class AuthorizationServer
    {
        public string Name { get; set; }

        public AuthorizationServerType Type { get; set; }
    }
}
