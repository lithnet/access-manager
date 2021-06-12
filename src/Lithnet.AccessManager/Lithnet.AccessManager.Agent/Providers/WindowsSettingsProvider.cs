using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lithnet.AccessManager.Agent.Configuration;
using Microsoft.Extensions.Options;

namespace Lithnet.AccessManager.Agent.Providers
{
    public class WindowsSettingsProvider : ISettingsProvider
    {
        private readonly RegistrySettingsProvider registrySettingsAgent;
        private readonly RegistrySettingsProvider registrySettingsPassword;
        private readonly IOptionsMonitor<AgentOptions> agentOptions;

        public WindowsSettingsProvider(IOptionsMonitor<AgentOptions> agentOptions)
        {
            this.agentOptions = agentOptions;
            this.registrySettingsAgent = new RegistrySettingsProvider("Lithnet\\Access Manager Agent", true);
            this.registrySettingsPassword = new RegistrySettingsProvider("Lithnet\\Access Manager Agent\\Password", true);
        }

        public int Interval => this.registrySettingsAgent.GetValue<int>("Interval", agentOptions.CurrentValue.Interval);

        public bool Enabled => this.registrySettingsAgent.GetValue<bool>("Enabled", agentOptions.CurrentValue.Enabled);

        public bool AdvancedAgentEnabled => this.registrySettingsAgent.GetValue<bool>("AdvancedAgent", agentOptions.CurrentValue.AdvancedAgent.Enabled);

        public AuthenticationMode AuthenticationMode => this.registrySettingsAgent.GetValue<AuthenticationMode>("AuthenticationMode", agentOptions.CurrentValue.AdvancedAgent.AuthenticationMode);


        public string Server => this.registrySettingsAgent.GetValue<string>("Server", agentOptions.CurrentValue.AdvancedAgent.Server);

        public bool PasswordManagementEnabled => this.registrySettingsPassword.GetValue<bool>("Enabled", agentOptions.CurrentValue.Enabled);

        public int PasswordLength => this.registrySettingsPassword.GetValue<int>("PasswordLength", agentOptions.CurrentValue.PasswordManagement.PasswordPolicy.PasswordLength);

        public string PasswordCharacters => this.registrySettingsPassword.GetValue<string>("PasswordCharacters", agentOptions.CurrentValue.PasswordManagement.PasswordPolicy.PasswordCharacters);

        public bool UseUpper => this.registrySettingsPassword.GetValue<bool>("UseUpper", agentOptions.CurrentValue.PasswordManagement.PasswordPolicy.UseUpper);

        public bool UseLower => this.registrySettingsPassword.GetValue<bool>("UseLower", agentOptions.CurrentValue.PasswordManagement.PasswordPolicy.UseLower);

        public bool UseSymbol => this.registrySettingsPassword.GetValue<bool>("UseSymbol", agentOptions.CurrentValue.PasswordManagement.PasswordPolicy.UseSymbol);

        public bool UseNumeric => this.registrySettingsPassword.GetValue<bool>("UseNumeric", agentOptions.CurrentValue.PasswordManagement.PasswordPolicy.UseNumeric);

        public int LithnetLocalAdminPasswordHistoryDaysToKeep => this.registrySettingsPassword.GetValue<int>("PasswordHistoryDaysToKeep", agentOptions.CurrentValue.PasswordManagement.ActiveDirectorySettings.LithnetLocalAdminPasswordHistoryDaysToKeep);

        public PasswordAttributeBehaviour LithnetLocalAdminPasswordAttributeBehaviour => this.registrySettingsPassword.GetValue<PasswordAttributeBehaviour>("LithnetLocalAdminPasswordBehaviour", agentOptions.CurrentValue.PasswordManagement.ActiveDirectorySettings.LithnetLocalAdminPasswordAttributeBehaviour);

        public PasswordAttributeBehaviour MsMcsAdmPwdAttributeBehaviour => this.registrySettingsPassword.GetValue<PasswordAttributeBehaviour>("MsMcsAdmPwdBehaviour", agentOptions.CurrentValue.PasswordManagement.ActiveDirectorySettings.MsMcsAdmPwdAttributeBehaviour);

        public int MaximumPasswordAge => this.registrySettingsPassword.GetValue<int>("MaximumPasswordAge", agentOptions.CurrentValue.PasswordManagement.PasswordPolicy.MaximumPasswordAge);

        public IList<string> AadIssuerDNs => this.registrySettingsAgent.GetValue<string>("Server", null)?.Split(";").ToList<string>() ?? agentOptions.CurrentValue.AdvancedAgent.AadIssuerDNs;

    }
}
