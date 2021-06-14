using System;
using Lithnet.AccessManager.Agent.Configuration;
using Microsoft.Extensions.Options;

namespace Lithnet.AccessManager.Agent.Providers
{
    public class SettingsProvider : ISettingsProvider
    {
        private readonly IOptionsMonitor<AgentOptions> agentOptions;
        private readonly IWritableOptions<AppState> appState;

        public SettingsProvider(IOptionsMonitor<AgentOptions> agentOptions, IWritableOptions<AppState> appState)
        {
            this.agentOptions = agentOptions;
            this.appState = appState;
        }

        public int Interval => this.agentOptions.CurrentValue.Interval;

        public bool Enabled => this.agentOptions.CurrentValue.Enabled;

        public bool AdvancedAgentEnabled => this.agentOptions.CurrentValue.AdvancedAgent.Enabled;

        public AuthenticationMode AuthenticationMode => this.agentOptions.CurrentValue.AdvancedAgent.AuthenticationMode;

        public string Server => this.agentOptions.CurrentValue.AdvancedAgent.Server;

        public bool PasswordManagementEnabled => this.agentOptions.CurrentValue.PasswordManagement.Enabled;

        public int PasswordLength => this.agentOptions.CurrentValue.PasswordManagement.PasswordPolicy.PasswordLength;

        public string PasswordCharacters => this.agentOptions.CurrentValue.PasswordManagement.PasswordPolicy.PasswordCharacters;

        public bool UseUpper => this.agentOptions.CurrentValue.PasswordManagement.PasswordPolicy.UseUpper;

        public bool UseLower => this.agentOptions.CurrentValue.PasswordManagement.PasswordPolicy.UseLower;

        public bool UseSymbol => this.agentOptions.CurrentValue.PasswordManagement.PasswordPolicy.UseSymbol;

        public bool UseNumeric => this.agentOptions.CurrentValue.PasswordManagement.PasswordPolicy.UseNumeric;

        public int MaximumPasswordAgeDays => this.agentOptions.CurrentValue.PasswordManagement.PasswordPolicy.MaximumPasswordAgeDays;

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

        public string CheckRegistrationUrl
        {
            get => this.appState.Value.CheckRegistrationUrl;
            set
            {
                this.appState.Update(t => t.CheckRegistrationUrl = value);
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

        public int CheckInIntervalHours => this.agentOptions.CurrentValue.CheckInIntervalHours;

        public int LithnetLocalAdminPasswordHistoryDaysToKeep => this.agentOptions.CurrentValue.PasswordManagement.ActiveDirectorySettings.LithnetLocalAdminPasswordHistoryDaysToKeep;

        public PasswordAttributeBehaviour LithnetLocalAdminPasswordAttributeBehaviour => this.agentOptions.CurrentValue.PasswordManagement.ActiveDirectorySettings.LithnetLocalAdminPasswordAttributeBehaviour;

        public PasswordAttributeBehaviour MsMcsAdmPwdAttributeBehaviour => this.agentOptions.CurrentValue.PasswordManagement.ActiveDirectorySettings.MsMcsAdmPwdAttributeBehaviour;
    }
}
