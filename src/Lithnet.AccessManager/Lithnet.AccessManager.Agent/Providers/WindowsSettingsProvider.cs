using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lithnet.AccessManager.Agent.Configuration;
using Lithnet.AccessManager.Api.Shared;
using Microsoft.Extensions.Options;

namespace Lithnet.AccessManager.Agent.Providers
{
    public class WindowsSettingsProvider : ISettingsProvider
    {
        private readonly RegistrySettingsProvider policySettingsAgent;
        private readonly RegistrySettingsProvider policySettingsPassword;
        private readonly RegistrySettingsProvider registrySettingsAgent;
        private readonly IOptionsMonitor<AgentOptions> agentOptions;

        public WindowsSettingsProvider(IOptionsMonitor<AgentOptions> agentOptions)
        {
            this.agentOptions = agentOptions;
            this.policySettingsAgent = new RegistrySettingsProvider("Policies\\Lithnet\\Access Manager Agent");
            this.policySettingsPassword = new RegistrySettingsProvider("Policies\\Lithnet\\Access Manager Agent\\Password");
            this.registrySettingsAgent = new RegistrySettingsProvider("Lithnet\\Access Manager Agent");
        }

        public int Interval => this.policySettingsAgent.GetValue<int>("Interval", agentOptions.CurrentValue.Interval);

        public bool Enabled => this.policySettingsAgent.GetValue<bool>("Enabled", agentOptions.CurrentValue.Enabled);

        public bool AdvancedAgentEnabled => this.policySettingsAgent.GetValue<bool>("AdvancedAgent", agentOptions.CurrentValue.AdvancedAgent.Enabled);

        public AgentAuthenticationMode AuthenticationMode => this.policySettingsAgent.GetValue<AgentAuthenticationMode>("AuthenticationMode", agentOptions.CurrentValue.AdvancedAgent.AuthenticationMode);


        public string Server => this.policySettingsAgent.GetValue<string>("Server", agentOptions.CurrentValue.AdvancedAgent.Server);

        public bool PasswordManagementEnabled => this.policySettingsPassword.GetValue<bool>("Enabled", agentOptions.CurrentValue.Enabled);

        public int PasswordLength => this.policySettingsPassword.GetValue<int>("PasswordLength", agentOptions.CurrentValue.PasswordManagement.PasswordPolicy.PasswordLength);

        public string PasswordCharacters => this.policySettingsPassword.GetValue<string>("PasswordCharacters", agentOptions.CurrentValue.PasswordManagement.PasswordPolicy.PasswordCharacters);

        public bool UseUpper => this.policySettingsPassword.GetValue<bool>("UseUpper", agentOptions.CurrentValue.PasswordManagement.PasswordPolicy.UseUpper);

        public bool UseLower => this.policySettingsPassword.GetValue<bool>("UseLower", agentOptions.CurrentValue.PasswordManagement.PasswordPolicy.UseLower);

        public bool UseSymbol => this.policySettingsPassword.GetValue<bool>("UseSymbol", agentOptions.CurrentValue.PasswordManagement.PasswordPolicy.UseSymbol);

        public bool UseNumeric => this.policySettingsPassword.GetValue<bool>("UseNumeric", agentOptions.CurrentValue.PasswordManagement.PasswordPolicy.UseNumeric);

        public int LithnetLocalAdminPasswordHistoryDaysToKeep => this.policySettingsPassword.GetValue<int>("PasswordHistoryDaysToKeep", agentOptions.CurrentValue.PasswordManagement.ActiveDirectorySettings.LithnetLocalAdminPasswordHistoryDaysToKeep);

        public PasswordAttributeBehaviour LithnetLocalAdminPasswordAttributeBehaviour => this.policySettingsPassword.GetValue<PasswordAttributeBehaviour>("LithnetLocalAdminPasswordBehaviour", agentOptions.CurrentValue.PasswordManagement.ActiveDirectorySettings.LithnetLocalAdminPasswordAttributeBehaviour);

        public PasswordAttributeBehaviour MsMcsAdmPwdAttributeBehaviour => this.policySettingsPassword.GetValue<PasswordAttributeBehaviour>("MsMcsAdmPwdBehaviour", agentOptions.CurrentValue.PasswordManagement.ActiveDirectorySettings.MsMcsAdmPwdAttributeBehaviour);

        public int MaximumPasswordAgeDays => this.policySettingsPassword.GetValue<int>("MaximumPasswordAge", agentOptions.CurrentValue.PasswordManagement.PasswordPolicy.MaximumPasswordAgeDays);

        public string RegistrationKey
        {
            get => this.registrySettingsAgent.GetValue<string>("RegistrationKey", null);
            set => this.registrySettingsAgent.SetValue("RegistrationKey", value);
        }

        public string ClientId
        {
            get => this.registrySettingsAgent.GetValue<string>("ClientId", null);
            set => this.registrySettingsAgent.SetValue("ClientId", value);
        }

        public string CheckRegistrationUrl
        {
            get => this.registrySettingsAgent.GetValue<string>("CheckRegistrationUrl", null);
            set => this.registrySettingsAgent.SetValue("CheckRegistrationUrl", value);
        }

        public string AuthCertificate
        {
            get => this.registrySettingsAgent.GetValue<string>("AuthCertificate", null);
            set => this.registrySettingsAgent.SetValue("AuthCertificate", value);
        }

        public DateTime LastCheckIn
        {
            get => new DateTime(this.registrySettingsAgent.GetValue<long>("LastCheckIn", 0));
            set => this.registrySettingsAgent.SetValue("LastCheckIn", value.Ticks);
        }

        public int CheckInIntervalHours => this.policySettingsAgent.GetValue<int>("CheckInIntervalHours", agentOptions.CurrentValue.CheckInIntervalHours);
        public TimeSpan MetadataCacheDuration => TimeSpan.FromHours(this.policySettingsAgent.GetValue<int>("MetadataCacheDurationHours", (int)agentOptions.CurrentValue.AdvancedAgent.MetadataCacheDuration.TotalHours));

        public RegistrationState RegistrationState
        {
            get => (RegistrationState)this.registrySettingsAgent.GetValue("RegistrationState", 0);
            set => this.registrySettingsAgent.SetValue("RegistrationState", (int)value);
        }
    }
}
