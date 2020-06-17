using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Lithnet.AccessManager
{
    public class PasswordHistoryEntry
    {
        [JsonProperty("data")]
        public string EncryptedData { get; set; }

        [JsonProperty("effectiveFrom")]
        public DateTime EffectiveFrom { get; set; }
    }
}
