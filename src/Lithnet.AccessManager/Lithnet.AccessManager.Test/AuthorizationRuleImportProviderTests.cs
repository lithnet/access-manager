using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace Lithnet.AccessManager.Test
{
    class AuthorizationRuleImportProviderTests
    {
        private IDirectory directory;

        private IDiscoveryServices discoveryServices;

        private AuthorizationRuleImportProvider provider;

        private ILogger<AuthorizationRuleImportProvider> logger;

        private ILocalSam localSam;

        public AuthorizationRuleImportProviderTests()
        {
        }

        [SetUp()]
        public void TestInitialize()
        {
            this.discoveryServices = new DiscoveryServices(Global.LogFactory.CreateLogger<DiscoveryServices>());
            directory = new ActiveDirectory(discoveryServices);
            localSam = new LocalSam(Global.LogFactory.CreateLogger<LocalSam>());
            logger = Global.LogFactory.CreateLogger<AuthorizationRuleImportProvider>();
            IComputerPrincipalProviderBitLocker bitLockerProvider = new ComputerPrincipalProviderBitLocker(discoveryServices, Global.LogFactory.CreateLogger<ComputerPrincipalProviderBitLocker>());
            IComputerPrincipalProviderMsLaps lapsProvider = new ComputerPrincipalProviderMsLaps(discoveryServices, Global.LogFactory.CreateLogger<ComputerPrincipalProviderMsLaps>());
            IComputerPrincipalProviderRpc rpcProvider = new ComputerPrincipalProviderRpc( localSam, directory, Global.LogFactory.CreateLogger<ComputerPrincipalProviderRpc>());
            IComputerPrincipalProviderCsv csvProvider = new ComputerPrincipalProviderCsv(directory, Global.LogFactory.CreateLogger<ComputerPrincipalProviderCsv>());

            provider = new AuthorizationRuleImportProvider(logger, directory, csvProvider, rpcProvider, lapsProvider, bitLockerProvider);
        }

        [Test]
        public void TestImportFromCSV()
        {
            AuthorizationRuleImportSettings settings = new AuthorizationRuleImportSettings()
            {
                DiscoveryMode = ImportMode.File,
                ImportFile = "TestFiles\\ComputerList.csv",
                ImportOU = "OU=Admin,DC=IDMDEV1,DC=LOCAL"
            };
            
            var cache = provider.BuildPrincipalMap(settings);

            provider.WriteReport(cache, "D:\\dev\\temp\\output-1.txt");
        }
  
        [Test]
        public void TestImportFromBitLocker()
        {
            AuthorizationRuleImportSettings settings = new AuthorizationRuleImportSettings()
            {
                DiscoveryMode = ImportMode.BitLocker,
                ImportOU = "OU=Admin,DC=IDMDEV1,DC=LOCAL"
            };

            var cache = provider.BuildPrincipalMap(settings);

            provider.WriteReport(cache, "D:\\dev\\temp\\output-2.txt");
        }

        [Test]
        public void TestImportFromMsLaps()
        {
            AuthorizationRuleImportSettings settings = new AuthorizationRuleImportSettings()
            {
                DiscoveryMode = ImportMode.Laps,
                ImportOU = "OU=Admin,DC=IDMDEV1,DC=LOCAL"
            };

            var cache = provider.BuildPrincipalMap(settings);

            provider.WriteReport(cache, "D:\\dev\\temp\\output-3.txt");
        }
    }
}
