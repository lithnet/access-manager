using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Lithnet.AccessManager
{
    public class ProtectedPasswordHistoryItem
    {
        [JsonProperty("data")]
        public string EncryptedData { get; set; }

        [JsonProperty("created")]
        public DateTime Created { get; set; }

        [JsonProperty("retired")]
        public DateTime? Retired { get; set; }
    }
}
