namespace Lithnet.AccessManager.Agent.Providers
{
    public interface IActiveDirectoryLapsSettingsProvider : IPasswordPolicy
    {
        bool Enabled { get; }

        int PasswordHistoryDaysToKeep { get; }

        PasswordAttributeBehaviour MsMcsAdmPwdBehaviour { get; }
    }
}