using System;
using Microsoft.Extensions.Logging;

namespace Lithnet.AccessManager.Server.UI.AuthorizationRuleImport
{
    public class ImportProviderLaps : ImportProviderComputerDiscovery
    {
        private readonly ILogger logger;
        private readonly IDirectory directory;
        private readonly IComputerPrincipalProvider provider;
        private readonly ImportSettingsLaps settings;

        public ImportProviderLaps(ImportSettingsLaps settings, ILogger<ImportProviderLaps> logger, IDirectory directory, IComputerPrincipalProviderLaps provider)
            : base(settings, logger, directory)
        {
            this.logger = logger;
            this.directory = directory;
            this.provider = provider;
            this.settings = settings;
        }

        public override ImportResults Import()
        {
            if (settings.ImportMode != ImportMode.Laps)
            {
                throw new InvalidOperationException("The incorrect settings were provided");
            }

            return this.PerformComputerDiscovery(this.provider);
        }
    }
}