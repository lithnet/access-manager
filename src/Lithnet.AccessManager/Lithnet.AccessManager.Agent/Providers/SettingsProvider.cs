using System;
using System.Collections.Generic;
using System.Text;
using Lithnet.AccessManager.Agent.Configuration;
using Microsoft.Extensions.Options;

namespace Lithnet.AccessManager.Agent.Providers
{
    public class SettingsProvider : ISettingsProvider
    {
        private readonly IOptionsMonitor<AgentOptions> agentOptions;

        public SettingsProvider(IOptionsMonitor<AgentOptions> agentOptions)
        {
            this.agentOptions = agentOptions;
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

        public int MaximumPasswordAge => this.agentOptions.CurrentValue.PasswordManagement.PasswordPolicy.MaximumPasswordAge;
        
        public IList<string> AadIssuerDNs => this.agentOptions.CurrentValue.AdvancedAgent.AadIssuerDNs;

        public int LithnetLocalAdminPasswordHistoryDaysToKeep => this.agentOptions.CurrentValue.PasswordManagement.ActiveDirectorySettings.LithnetLocalAdminPasswordHistoryDaysToKeep;

        public PasswordAttributeBehaviour LithnetLocalAdminPasswordAttributeBehaviour => this.agentOptions.CurrentValue.PasswordManagement.ActiveDirectorySettings.LithnetLocalAdminPasswordAttributeBehaviour;

        public PasswordAttributeBehaviour MsMcsAdmPwdAttributeBehaviour => this.agentOptions.CurrentValue.PasswordManagement.ActiveDirectorySettings.MsMcsAdmPwdAttributeBehaviour;

      
    }
}
