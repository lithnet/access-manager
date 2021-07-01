using System;
using System.Collections.Generic;
using Lithnet.AccessManager.Api.Shared;

namespace Lithnet.AccessManager.Agent.Providers
{
    public interface ISettingsProvider
    {
        int Interval { get; }

        bool Enabled { get; }

        bool AdvancedAgentEnabled { get; }

        AgentAuthenticationMode AuthenticationMode { get; }

        string Server { get; }

        bool PasswordManagementEnabled { get; }

        int PasswordLength { get; }

        string PasswordCharacters { get; }

        bool UseUpper { get; }

        bool UseLower { get; }

        bool UseSymbol { get; }

        bool UseNumeric { get; }

        int LithnetLocalAdminPasswordHistoryDaysToKeep { get; }

        PasswordAttributeBehaviour LithnetLocalAdminPasswordAttributeBehaviour { get; }

        PasswordAttributeBehaviour MsMcsAdmPwdAttributeBehaviour { get; }

        int MaximumPasswordAgeDays { get; }

        string RegistrationKey { get; set; }

        string ClientId { get; set; }

        string CheckRegistrationUrl { get; set; }

        RegistrationState RegistrationState { get; set; }

        string AuthCertificate { get; set; }

        DateTime LastCheckIn { get; set; }

        int CheckInIntervalHours { get; }

        TimeSpan MetadataCacheDuration { get; }

        bool HasRegisteredSecondaryCredentials { get; set; }
        bool RegisterSecondaryCredentialsForAadr { get; }

        bool RegisterSecondaryCredentialsForAadj { get; }
    }
}