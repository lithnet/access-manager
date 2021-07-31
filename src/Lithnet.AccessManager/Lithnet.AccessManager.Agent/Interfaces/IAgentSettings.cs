using System;
using System.Collections.Generic;
using Lithnet.AccessManager.Api.Shared;

namespace Lithnet.AccessManager.Agent.Providers
{
    public interface IAgentSettings
    {
        int Interval { get; }

        bool Enabled { get; }

        bool AmsPasswordStorageEnabled { get; }

        bool AmsServerManagementEnabled { get; }

        AgentAuthenticationMode AuthenticationMode { get; }

        string Server { get; }

        IEnumerable<string> AzureAdTenantIDs { get; }

        int CheckInIntervalHours { get; }

        bool RegisterSecondaryCredentialsForAadr { get; }

        bool RegisterSecondaryCredentialsForAadj { get; }

        string RegistrationKey { get; set; }

        string ClientId { get; set; }

        RegistrationState RegistrationState { get; set; }

        string AuthCertificate { get; set; }

        DateTime LastCheckIn { get; set; }

        bool HasRegisteredSecondaryCredentials { get; set; }

        bool EnableAdminAccount { get; }

        bool Reset { get; set; }

        void Clear();
    }
}