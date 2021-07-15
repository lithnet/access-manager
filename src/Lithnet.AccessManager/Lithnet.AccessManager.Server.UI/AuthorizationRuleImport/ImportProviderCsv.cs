using System;
using Microsoft.Extensions.Logging;

namespace Lithnet.AccessManager.Server.UI.AuthorizationRuleImport
{
    public class ImportProviderCsv : ImportProviderComputerDiscovery
    {
        private readonly ILogger logger;
        private readonly IActiveDirectory directory;
        private readonly IComputerPrincipalProviderCsv provider;
        private readonly ImportSettingsCsv settings;

        public ImportProviderCsv(ImportSettingsCsv settings, ILogger<ImportProviderCsv> logger, IActiveDirectory directory, IComputerPrincipalProviderCsv provider)
            : base(settings, logger, directory)
        {
            this.logger = logger;
            this.directory = directory;
            this.provider = provider;
            this.settings = settings;
        }

        public override ImportResults Import()
        {
            if (settings.ImportMode != ImportMode.CsvFile)
            {
                throw new InvalidOperationException("The incorrect settings were provided");
            }

            this.provider.ImportPrincipalMappings(settings.ImportFile, settings.HasHeaderRow);

            return this.PerformComputerDiscovery(this.provider);
        }
    }
}