using System;
using Lithnet.AccessManager.Server.UI.AuthorizationRuleImport;
using Microsoft.Extensions.Logging;

namespace Lithnet.AccessManager.Server.UI
{
    public class ImportProviderFactory : IImportProviderFactory
    {
        private readonly ILogger<ImportProviderBitLocker> loggerBitLocker;
        private readonly ILogger<ImportProviderCsv> loggerCsv;
        private readonly ILogger<ImportProviderLaps> loggerLaps;
        private readonly ILogger<ImportProviderLapsWeb> loggerLapsWeb;
        private readonly ILogger<ImportProviderRpc> loggerRpc;
        private readonly IActiveDirectory directory;
        private readonly IComputerPrincipalProviderBitLocker providerBitLocker;
        private readonly IComputerPrincipalProviderCsv providerCsv;
        private readonly IComputerPrincipalProviderLaps providerLaps;
        private readonly IComputerPrincipalProviderRpc providerRpc;

        public ImportProviderFactory(ILogger<ImportProviderBitLocker> loggerBitLocker, ILogger<ImportProviderCsv> loggerCsv, ILogger<ImportProviderLaps> loggerLaps, ILogger<ImportProviderLapsWeb> loggerLapsWeb, ILogger<ImportProviderRpc> loggerRpc, IActiveDirectory directory, IComputerPrincipalProviderBitLocker providerBitLocker, IComputerPrincipalProviderCsv providerCsv, IComputerPrincipalProviderLaps providerLaps, IComputerPrincipalProviderRpc providerRpc)
        {
            this.loggerBitLocker = loggerBitLocker;
            this.loggerCsv = loggerCsv;
            this.loggerLaps = loggerLaps;
            this.loggerLapsWeb = loggerLapsWeb;
            this.loggerRpc = loggerRpc;
            this.directory = directory;
            this.providerBitLocker = providerBitLocker;
            this.providerCsv = providerCsv;
            this.providerLaps = providerLaps;
            this.providerRpc = providerRpc;
        }

        public IImportProvider CreateImportProvider(ImportSettings settings)
        {
            switch (settings.ImportMode)
            {
                case ImportMode.BitLocker:
                    return new ImportProviderBitLocker(settings as ImportSettingsBitLocker ?? throw new InvalidOperationException("Wrong settings type was provided"), loggerBitLocker, directory, providerBitLocker);

                case ImportMode.CsvFile:
                    return new ImportProviderCsv(settings as ImportSettingsCsv ?? throw new InvalidOperationException("Wrong settings type was provided"), loggerCsv, directory, providerCsv);

                case ImportMode.Laps:
                    return new ImportProviderLaps(settings as ImportSettingsLaps ?? throw new InvalidOperationException("Wrong settings type was provided"), loggerLaps, directory, providerLaps);

                case ImportMode.LapsWeb:
                    return new ImportProviderLapsWeb(settings as ImportSettingsLapsWeb ?? throw new InvalidOperationException("Wrong settings type was provided"), loggerLapsWeb, directory);

                case ImportMode.Rpc:
                    return new ImportProviderRpc(settings as ImportSettingsRpc ?? throw new InvalidOperationException("Wrong settings type was provided"), loggerRpc, directory, providerRpc);
            }

            throw new InvalidOperationException("Unknown import type");
        }
    }
}

