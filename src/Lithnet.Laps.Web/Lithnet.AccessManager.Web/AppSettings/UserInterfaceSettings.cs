using Lithnet.AccessManager.Web.Internal;
using Microsoft.Extensions.Configuration;

namespace Lithnet.AccessManager.Web.AppSettings
{
    public class UserInterfaceSettings : IUserInterfaceSettings
    {
        private readonly IConfiguration configuration;

        public UserInterfaceSettings(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public string Title => this.configuration["user-interface:title"] ?? "Lithnet Access Manager";

        public AuditReasonFieldState UserSuppliedReason => this.configuration.GetValueOrDefault("user-interface:user-supplied-reason", AuditReasonFieldState.Optional);

        public bool AllowLaps => this.configuration.GetValueOrDefault("user-interface:allow-laps", true);

        public bool AllowJit => this.configuration.GetValueOrDefault("user-interface:allow-jit", true);

        public bool AllowLapsHistory => this.configuration.GetValueOrDefault("user-interface:allow-laps-history", true);
    }
}