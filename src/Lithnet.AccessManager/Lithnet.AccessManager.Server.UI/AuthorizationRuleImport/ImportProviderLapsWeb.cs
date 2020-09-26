using System;
using Microsoft.Extensions.Logging;

namespace Lithnet.AccessManager.Server.UI.AuthorizationRuleImport
{
    public class ImportProviderLapsWeb : IImportProvider
    {
        private readonly ILogger logger;
        private readonly IDirectory directory;
        private readonly ImportSettingsLapsWeb settings;

        public event EventHandler<ImportProcessingEventArgs> OnItemProcessStart;

        public event EventHandler<ImportProcessingEventArgs> OnItemProcessFinish;

        public ImportProviderLapsWeb(ImportSettingsLapsWeb settings, ILogger<ImportProviderLapsWeb> logger, IDirectory directory)
        {
            this.logger = logger;
            this.directory = directory;
            this.settings = settings;
        }

        public int GetEstimatedItemCount()
        {
            throw new NotImplementedException();
        }

        public ImportResults Import()
        {
            throw new NotImplementedException();
        }
    }
}
