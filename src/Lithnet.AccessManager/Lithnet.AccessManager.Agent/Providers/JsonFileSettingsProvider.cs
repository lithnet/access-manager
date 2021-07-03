using System;
using System.Collections.Generic;
using Lithnet.AccessManager.Agent.Configuration;
using Lithnet.AccessManager.Api.Shared;
using Microsoft.Extensions.Options;

namespace Lithnet.AccessManager.Agent.Providers
{
    public class JsonFileSettingsProvider : IAgentSettings
    {
        private readonly IOptionsMonitor<AgentOptions> agentOptions;
        private readonly IWritableOptions<AppState> appState;

        public JsonFileSettingsProvider(IOptionsMonitor<AgentOptions> agentOptions, IWritableOptions<AppState> appState)
        {
            this.agentOptions = agentOptions;
            this.appState = appState;
        }

        public int Interval => this.agentOptions.CurrentValue.Interval;

        public bool AmsPasswordStorageEnabled => this.agentOptions.CurrentValue.AmsPasswordStorageEnabled;

        public bool AmsServerManagementEnabled => this.agentOptions.CurrentValue.Enabled;
        
        public bool Enabled => this.agentOptions.CurrentValue.Enabled;

        public AgentAuthenticationMode AuthenticationMode => this.agentOptions.CurrentValue.AuthenticationMode;

        public string Server => this.agentOptions.CurrentValue.Server;

        public IEnumerable<string> AzureAdTenantIDs => this.agentOptions.CurrentValue.AzureTenantIDs;

        public bool RegisterSecondaryCredentialsForAadr => this.agentOptions.CurrentValue.RegisterSecondaryCredentialsForAadr;

        public bool RegisterSecondaryCredentialsForAadj => this.agentOptions.CurrentValue.RegisterSecondaryCredentialsForAadj;

        public int CheckInIntervalHours => this.agentOptions.CurrentValue.CheckInIntervalHours;

        public TimeSpan MetadataCacheDuration => this.agentOptions.CurrentValue.MetadataCacheDuration;

        public bool EnableAdminAccount => this.agentOptions.CurrentValue.EnableAdminAccount;
        
        public string RegistrationKey
        {
            get => this.appState.Value.RegistrationKey;
            set
            {
                this.appState.Update(t => t.RegistrationKey = value);
            }
        }

        public string ClientId
        {
            get => this.appState.Value.ClientId;
            set
            {
                this.appState.Update(t => t.ClientId = value);
            }
        }

        public RegistrationState RegistrationState
        {
            get => this.appState.Value.RegistrationState;
            set
            {
                this.appState.Update(t => t.RegistrationState = value);
            }
        }

        public string AuthCertificate
        {
            get => this.appState.Value.AuthCertificate;
            set
            {
                this.appState.Update(t => t.AuthCertificate = value);
            }
        }

        public DateTime LastCheckIn
        {
            get => this.appState.Value.LastCheckIn;
            set
            {
                this.appState.Update(t => t.LastCheckIn = value);
            }
        }

        public bool HasRegisteredSecondaryCredentials
        {
            get => this.appState.Value.HasRegisteredSecondaryCredentials;
            set
            {
                this.appState.Update(t => t.HasRegisteredSecondaryCredentials = value);
            }
        }
    }
}
