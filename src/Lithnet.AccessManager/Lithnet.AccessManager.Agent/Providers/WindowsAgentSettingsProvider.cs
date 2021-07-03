using Lithnet.AccessManager.Api.Shared;
using System;
using System.Collections.Generic;

namespace Lithnet.AccessManager.Agent.Providers
{
    public class WindowsAgentSettingsProvider : IAgentSettings
    {
        private readonly RegistrySettingsProvider policyAgent;
        private readonly RegistrySettingsProvider settingsAgent;
        private readonly RegistrySettingsProvider registryState;

        public WindowsAgentSettingsProvider(IRegistryPathProvider pathProvider)
        {
            this.policyAgent = new RegistrySettingsProvider(pathProvider.PolicyAgentPath);
            this.settingsAgent = new RegistrySettingsProvider(pathProvider.SettingsAgentPath);
            this.registryState = new RegistrySettingsProvider(pathProvider.StatePath);
        }

        public int Interval => this.policyAgent.GetValue<int>("Interval", this.settingsAgent.GetValue<int>("Interval", 60));

        public bool AmsServerManagementEnabled => this.policyAgent.GetValue<bool>("AmsServerManagementEnabled", this.settingsAgent.GetValue<bool>("AmsServerManagementEnabled", false));

        public bool Enabled => this.policyAgent.GetValue<bool>("Enabled", this.settingsAgent.GetValue<bool>("Enabled", false));

        public bool AmsPasswordStorageEnabled => this.policyAgent.GetValue<bool>("AmsPasswordStorageEnabled", this.settingsAgent.GetValue<bool>("AmsPasswordStorageEnabled", false));

        public AgentAuthenticationMode AuthenticationMode => this.policyAgent.GetValue<AgentAuthenticationMode>("AuthenticationMode", this.settingsAgent.GetValue<AgentAuthenticationMode>("AuthenticationMode", AgentAuthenticationMode.None));

        public string Server => this.policyAgent.GetValue<string>("Server", this.settingsAgent.GetValue<string>("Server", null));

        public string AzureAdTenantId => this.policyAgent.GetValue<string>("AzureAdTenantId", this.settingsAgent.GetValue<string>("AzureAdTenantId", null));

        public int CheckInIntervalHours => this.policyAgent.GetValue<int>("CheckInIntervalHours", this.settingsAgent.GetValue<int>("CheckInIntervalHours", 24));

        public bool RegisterSecondaryCredentialsForAadj => this.policyAgent.GetValue<bool>("RegisterSecondaryCredentialsForAadj", this.settingsAgent.GetValue<bool>("RegisterSecondaryCredentialsForAadj", false));

        public bool RegisterSecondaryCredentialsForAadr => this.policyAgent.GetValue<bool>("RegisterSecondaryCredentialsForAadr", this.settingsAgent.GetValue<bool>("RegisterSecondaryCredentialsForAadr", true));

        public TimeSpan MetadataCacheDuration => TimeSpan.FromHours(this.policyAgent.GetValue<int>("MetadataCacheDurationHours", this.settingsAgent.GetValue<int>("MetadataCacheDurationHours", 1)));

        public bool EnableAdminAccount => this.policyAgent.GetValue<bool>("EnableAdminAccount", this.settingsAgent.GetValue<bool>("EnableAdminAccount", true));

        public IEnumerable<string> AzureAdTenantIDs => this.AzureAdTenantId?.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries) ?? new string[0];

        public string RegistrationKey
        {
            get => this.registryState.GetValue<string>("RegistrationKey", null);
            set => this.registryState.SetValue("RegistrationKey", value);
        }

        public string ClientId
        {
            get => this.registryState.GetValue<string>("ClientId", null);
            set => this.registryState.SetValue("ClientId", value);
        }

        public string AuthCertificate
        {
            get => this.registryState.GetValue<string>("AuthCertificate", null);
            set => this.registryState.SetValue("AuthCertificate", value);
        }

        public DateTime LastCheckIn
        {
            get => new DateTime(this.registryState.GetValue<long>("LastCheckIn", 0));
            set => this.registryState.SetValue("LastCheckIn", value.Ticks);
        }

        public bool HasRegisteredSecondaryCredentials
        {
            get => this.registryState.GetValue<bool>("HasRegisteredSecondaryCredentials", false);
            set => this.registryState.SetValue("HasRegisteredSecondaryCredentials", value);
        }

        public RegistrationState RegistrationState
        {
            get => (RegistrationState)this.registryState.GetValue("RegistrationState", 0);
            set => this.registryState.SetValue("RegistrationState", (int)value);
        }
    }
}
