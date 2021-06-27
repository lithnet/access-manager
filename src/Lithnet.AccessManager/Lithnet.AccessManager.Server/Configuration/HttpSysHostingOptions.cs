using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Lithnet.AccessManager.Server.Configuration
{
    public class HttpSysHostingOptions
    {
        public static Guid AppId = new Guid("{4C3E21BA-7BEF-46C8-BC85-A4407DB6F596}");

        public int HttpPort { get; set; } = 80;

        public int HttpsPort { get; set; } = 443;

        public string Hostname { get; set; }

        public string ApiHostname { get; set; }

        public string Path { get; set; }

        public int MaxAccepts { get; set; }

        public bool EnableResponseCaching { get; set; }

        public bool ThrowWriteExceptions { get; set; }

        public long? MaxConnections { get; set; }

        public long RequestQueueLimit { get; set; }

        public long? MaxRequestBodySize { get; set; }

        public bool AllowSynchronousIO { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public Http503VerbosityLevel Http503Verbosity { get; set; }

        public string BuildApiHostUrl(string audiencePath = null)
        {
            if (string.IsNullOrWhiteSpace(this.ApiHostname))
            {
                throw new ConfigurationException("The API hostname is not set in the application configuration");
            }

            string host = this.ApiHostname;
            string port = this.HttpsPort == 443 ? string.Empty : ":" + this.HttpsPort.ToString();

            return $"https://{host}{port}/{audiencePath}";
        }

        public string BuildHttpUrlPrefix()
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

        public string BuildHttpsUrlPrefix()
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