using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Server.HttpSys;

namespace Lithnet.AccessManager.Configuration
{
    public class HttpSysHostingOptions : HttpSysOptions
    {
        public static Guid AppId = new Guid("{4C3E21BA-7BEF-46C8-BC85-A4407DB6F596}");

        public int HttpPort { get; set; } = 80;

        public int HttpsPort { get; set; } = 443;

        public string Hostname { get; set; }

        public string Path { get; set; }

        public  string BuildHttpUrlPrefix()
        {
            string host = "+";
            int port = 80;

            if (!string.IsNullOrWhiteSpace(this.Hostname))
            {
                host = this.Hostname;
            }

            if (this.HttpPort > 0)
            {
                port = this.HttpPort;
            }

            return BuildPrefix(host, port, this.Path, false);
        }

        public  string BuildHttpsUrlPrefix()
        {
            string host = "+";
            int port = 443;

            if (!string.IsNullOrWhiteSpace(this.Hostname))
            {
                host = this.Hostname;
            }

            if (this.HttpsPort > 0)
            {
                port = this.HttpsPort;
            }

            return BuildPrefix(host, port, this.Path, true);
        }

        public static string BuildPrefix(string host, int port, string path, bool isHttps)
        {
            return $"http{(isHttps ? "s" : null)}://{host}:{port}/{path}";
        }
    }
}