namespace Lithnet.AccessManager.Server.UI.AuthorizationRuleImport
{
    public class ImportProcessingEventArgs
    {
        public ImportProcessingEventArgs()
        {
        }

        public ImportProcessingEventArgs(string message)
        {
            this.Message = message;
        }

        public string Message { get; private set; }
    }
}
