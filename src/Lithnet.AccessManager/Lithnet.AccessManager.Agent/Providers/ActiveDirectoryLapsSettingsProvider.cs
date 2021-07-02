using Lithnet.AccessManager.Agent.Configuration;
using Lithnet.AccessManager.Api.Shared;
using Microsoft.Extensions.Options;
using System;

namespace Lithnet.AccessManager.Agent.Providers
{
    public class ActiveDirectoryLapsSettingsProvider : IActiveDirectoryLapsSettingsProvider
    {
        private readonly RegistrySettingsProvider policyPassword;

        public ActiveDirectoryLapsSettingsProvider(IRegistryPathProvider pathProvider)
        {
            this.policyPassword = new RegistrySettingsProvider(pathProvider.PolicyPasswordPath);
        }

        public bool Enabled => this.policyPassword.GetValue<bool>("Enabled", false);

        public int PasswordLength => this.policyPassword.GetValue<int>("PasswordLength", 16);

        public string PasswordCharacters => this.policyPassword.GetValue<string>("PasswordCharacters", null);

        public bool UseUpper => this.policyPassword.GetValue<bool>("UseUpper", true);

        public bool UseLower => this.policyPassword.GetValue<bool>("UseLower", true);

        public bool UseSymbol => this.policyPassword.GetValue<bool>("UseSymbol", false);

        public bool UseNumeric => this.policyPassword.GetValue<bool>("UseNumeric", true);

        public int PasswordHistoryDaysToKeep => this.policyPassword.GetValue<int>("PasswordHistoryDaysToKeep", 0);

        public PasswordAttributeBehaviour MsMcsAdmPwdBehaviour => this.policyPassword.GetValue<PasswordAttributeBehaviour>("MsMcsAdmPwdBehaviour", PasswordAttributeBehaviour.Ignore);

        public int MaximumPasswordAgeDays => this.policyPassword.GetValue<int>("MaximumPasswordAge", 7);

        public int MinimumNumberOfPasswords { get; } = 0;

        public int MinimumPasswordHistoryAgeDays { get; } = 0;
    }
}
