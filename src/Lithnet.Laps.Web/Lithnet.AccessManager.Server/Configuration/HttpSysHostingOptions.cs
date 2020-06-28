using System.Collections.Generic;
using Microsoft.AspNetCore.Server.HttpSys;

namespace Lithnet.AccessManager.Configuration
{
    public class HttpSysHostingOptions : HttpSysOptions
    {
        public IList<string> Urls { get; set; }
    }
}