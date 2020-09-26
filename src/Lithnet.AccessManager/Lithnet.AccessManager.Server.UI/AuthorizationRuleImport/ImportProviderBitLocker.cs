using System;
using Microsoft.Extensions.Logging;

namespace Lithnet.AccessManager.Server.UI.AuthorizationRuleImport
{
    public class ImportProviderBitLocker : ImportProviderComputerDiscovery
    {
        private readonly ILogger logger;
        private readonly IDirectory directory;
        private readonly IComputerPrincipalProvider provider;
        private readonly ImportSettingsBitLocker settings;

        public ImportProviderBitLocker(ImportSettingsBitLocker settings, ILogger<ImportProviderBitLocker> logger, IDirectory directory, IComputerPrincipalProviderBitLocker provider)
        : base(settings, logger, directory)
        {
            this.logger = logger;
            this.directory = directory;
            this.provider = provider;
            this.settings = settings;
        }

        public override ImportResults Import()
        {
            if (settings.ImportMode != ImportMode.BitLocker)
            {
                throw new InvalidOperationException("The incorrect settings were provided");
            }

            return this.PerformComputerDiscovery(this.provider);
        }
    }
}
