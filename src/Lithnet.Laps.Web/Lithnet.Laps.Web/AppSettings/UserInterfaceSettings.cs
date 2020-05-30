using System;
using Lithnet.Laps.Web.Config;
using Microsoft.Extensions.Configuration;

namespace Lithnet.Laps.Web.AppSettings
{
    public class UserInterfaceSettings : IUserInterfaceSettings
    {
        private IConfigurationRoot configuration;

        public UserInterfaceSettings(IConfigurationRoot configuration)
        {
            this.configuration = configuration;
        }

        public string Title => this.configuration["user-interface:title"] ?? "Lithnet LAPS Web App";

        public AuditReasonFieldState UserSuppliedReason
        {
            get
            {
                string value = this.configuration["user-interface:userSuppliedReason"];

                if (Enum.TryParse(value, true, out AuditReasonFieldState auditReasonFieldState))
                {
                    return auditReasonFieldState;
                }

                return AuditReasonFieldState.Optional;
            }
        }
    }
}