namespace Lithnet.AccessManager.Agent.Configuration
{
    public class ActiveDirectoryOptions
    {
        public PasswordAttributeBehaviour MsMcsAdmPwdAttributeBehaviour { get; set; }

        public PasswordAttributeBehaviour LithnetLocalAdminPasswordAttributeBehaviour { get; set; }

        public int LithnetLocalAdminPasswordHistoryDaysToKeep { get; set; }
    }
}