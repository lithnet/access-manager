using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Lithnet.AccessManager.Api.Shared
{
    public class AgentCheckIn
    {
        public string OperatingSystem { get; set; }

        public string OperatingSystemVersion { get; set; }

        public string AgentVersion { get; set; }

        public string Hostname { get; set; }

        public string DnsName { get; set; }

        public OsType OperatingSystemType { get; set; }

        public string ToHash()
        {
            var data = JsonSerializer.Serialize(this);

            using (var hasher = SHA1.Create())
            {
                return Convert.ToBase64String(hasher.ComputeHash(Encoding.UTF8.GetBytes(data)));
            }
        }
    }
}