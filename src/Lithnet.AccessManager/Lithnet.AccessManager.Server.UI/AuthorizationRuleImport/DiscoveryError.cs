namespace Lithnet.AccessManager.Server.UI.AuthorizationRuleImport
{
    public class DiscoveryError
    {
        public DiscoveryErrorType Type { get; set; }

        public string Computer { get; set; }

        public string Principal { get; set; }

        public string Message { get; set; }

        public bool IsError => this.Type == DiscoveryErrorType.Error;

        public bool IsWarning => this.Type == DiscoveryErrorType.Warning;

        public bool IsInformational => this.Type == DiscoveryErrorType.Informational;
    }
}
