using System;
using Microsoft.Extensions.Logging;

namespace Lithnet.AccessManager.Server.UI.AuthorizationRuleImport
{
    public class ImportProviderRpc : ImportProviderComputerDiscovery
    {
        private readonly ILogger logger;
        private readonly IActiveDirectory directory;
        private readonly IComputerPrincipalProvider provider;
        private readonly ImportSettingsRpc settings;

        public ImportProviderRpc(ImportSettingsRpc settings, ILogger<ImportProviderRpc> logger, IActiveDirectory directory, IComputerPrincipalProviderRpc provider)
            : base(settings, logger, directory)
        {
            this.logger = logger;
            this.directory = directory;
            this.provider = provider;
            this.settings = settings;
        }

        public override ImportResults Import()
        {
            if (settings.ImportMode != ImportMode.Rpc)
            {
                throw new InvalidOperationException("The incorrect settings were provided");
            }

            return this.PerformComputerDiscovery(this.provider);
        }
    }
}