using PropertyChanged;

namespace Lithnet.AccessManager.Server.UI.AuthorizationRuleImport
{
    [AddINotifyPropertyChangedInterface]
    public class DiscoveryError
    {
        public DiscoveryErrorType Type { get; set; }

        public string Target { get; set; }

        public string Principal { get; set; }

        public string Message { get; set; }

        [CsvHelper.Configuration.Attributes.Ignore]
        public bool IsError => this.Type == DiscoveryErrorType.Error;

        [CsvHelper.Configuration.Attributes.Ignore]
        public bool IsWarning => this.Type == DiscoveryErrorType.Warning;

        [CsvHelper.Configuration.Attributes.Ignore]
        public bool IsInformational => this.Type == DiscoveryErrorType.Informational;

        [CsvHelper.Configuration.Attributes.Ignore]
        public bool IsVisible { get; set; } = true;
    }
}
