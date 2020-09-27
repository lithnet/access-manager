namespace Lithnet.AccessManager.Server.UI.AuthorizationRuleImport
{
    public class ImportSettingsLapsWeb : ImportSettings
    {
        public string ImportFile { get; set; }

        public bool ImportNotifications { get; set; }

        public string SuccessTemplate { get; set; }

        public string FailureTemplate { get; set; }
    }
}
