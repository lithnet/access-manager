using System.ComponentModel.DataAnnotations;

namespace Lithnet.AccessManager.Server.Configuration
{
    public class JsonFileTargetsProviderOptions
    {
        public string AuthorizationFile { get; set; }

        public bool Enabled { get; set; } = true;
    }
}
