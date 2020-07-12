using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Lithnet.AccessManager.Server.Configuration
{
    public class UserInterfaceOptions
    {
        public string Title { get; set; } = "Lithnet Access Manager";

        [JsonConverter(typeof(StringEnumConverter))]
        public AuditReasonFieldState UserSuppliedReason { get; set; } = AuditReasonFieldState.Optional;

        public bool AllowLaps { get; set; } = true;

        public bool AllowJit { get; set; } = true;

        public bool AllowLapsHistory { get; set; } = true;

        public PhoneticSettings PhoneticSettings { get; set; } = new PhoneticSettings();
    }
}