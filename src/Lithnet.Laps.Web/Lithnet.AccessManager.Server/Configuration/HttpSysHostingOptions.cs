using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Server.HttpSys;

namespace Lithnet.AccessManager.Configuration
{
    public class HttpSysHostingOptions : HttpSysOptions
    {
        public static Guid AppId = new Guid("{4C3E21BA-7BEF-46C8-BC85-A4407DB6F596}");

        public string HttpUrl { get; set; }

        public string HttpsUrl { get; set; }
    }
}