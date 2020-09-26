namespace Lithnet.AccessManager.Server.UI.AuthorizationRuleImport
{
    public class ImportSettingsCsv : ImportSettingsComputerDiscovery
    {
        public string ImportFile { get; set; }

        public bool HasHeaderRow { get; set; }
    }
}
