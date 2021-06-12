using System.Collections.Generic;

namespace Lithnet.AccessManager.Agent.Providers
{
    public interface ISettingsProvider
    {
        int Interval { get; }

        bool Enabled { get; }

        bool AdvancedAgentEnabled { get; }

        AuthenticationMode AuthenticationMode { get; }

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

        int MaximumPasswordAge { get; }

        IList<string> AadIssuerDNs { get; }
    }
}