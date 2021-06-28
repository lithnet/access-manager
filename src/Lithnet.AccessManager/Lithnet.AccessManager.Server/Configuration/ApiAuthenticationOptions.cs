using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Api
{
    public class ApiAuthenticationOptions
    {
        [System.Text.Json.Serialization.JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public bool AllowAadAuth => this.AllowAzureAdJoinedDeviceAuth || this.AllowAzureAdRegisteredDeviceAuth;

        [System.Text.Json.Serialization.JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public bool AllowX509Auth => this.AllowAmsManagedDeviceAuth || this.AllowAadAuth;

        public bool AllowAmsManagedDeviceAuth { get; set; } = false;

        public bool AllowWindowsAuth { get; set; } = false;

        public bool AllowAzureAdJoinedDeviceAuth { get; set; } = false;

        public bool AllowAzureAdRegisteredDeviceAuth { get; set; } = false;
    }
}
