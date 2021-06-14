namespace Lithnet.AccessManager.Agent.Configuration
{
    public class ActiveDirectoryOptions
    {
        public PasswordAttributeBehaviour MsMcsAdmPwdAttributeBehaviour { get; set; } = PasswordAttributeBehaviour.Ignore;

        public PasswordAttributeBehaviour LithnetLocalAdminPasswordAttributeBehaviour { get; set; } = PasswordAttributeBehaviour.Ignore;

        public int LithnetLocalAdminPasswordHistoryDaysToKeep { get; set; } = 30;
    }
}