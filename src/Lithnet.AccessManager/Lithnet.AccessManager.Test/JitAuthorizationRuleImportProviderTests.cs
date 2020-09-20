using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace Lithnet.AccessManager.Test
{
    class JitAuthorizationRuleImportProviderTests
    {
        private IDirectory directory;

        private IDiscoveryServices discoveryServices;

        private AuthorizationRuleImportProvider provider;

        private ILogger<AuthorizationRuleImportProvider> logger;

        private ILocalSam localSam;

        public JitAuthorizationRuleImportProviderTests()
        {
        }

        [SetUp()]
        public void TestInitialize()
        {
            this.discoveryServices = new DiscoveryServices(Global.LogFactory.CreateLogger<DiscoveryServices>());
            directory = new ActiveDirectory(discoveryServices);
            localSam = new LocalSam(Global.LogFactory.CreateLogger<LocalSam>());
            logger = Global.LogFactory.CreateLogger<AuthorizationRuleImportProvider>();
            provider = new AuthorizationRuleImportProvider(logger, localSam, directory);
        }

        [Test]
        public void TestImportFromCSV()
        {
            OUPrincipalMapping entry = new OUPrincipalMapping()
            {
                AdsPath = "LDAP://ad.monash.edu/OU=Server Management,DC=AD,DC=MONASH,DC=EDU",
                OUName = "OU=Server Management,DC=AD,DC=MONASH,DC=EDU"
            };

            var cache = provider.LoadComputerPrincipalMapFromCsv("D:\\dev\\temp\\monash.csv");
            provider.BuildAdminMapViaCache(entry, cache, new List<System.Text.RegularExpressions.Regex>(), new List<System.Text.RegularExpressions.Regex>(), true, true);

            provider.WriteReport(entry, "D:\\dev\\temp\\monash.txt");
        }

        [Test]
        public void TestImportFromCSV2()
        {
            OUPrincipalMapping entry = new OUPrincipalMapping()
            {
                AdsPath = "LDAP://OU=Admin,DC=IDMDEV1,DC=LOCAL",
                OUName = "OU=Admin,DC=IDMDEV1,DC=LOCAL"
            };

            var cache = provider.LoadComputerPrincipalMapFromCsv("TestFiles\\ComputerList.csv");
            provider.BuildAdminMapViaCache(entry, cache, new List<System.Text.RegularExpressions.Regex>() { new Regex("Domain Admins") }, new List<System.Text.RegularExpressions.Regex>(), true, true);

            provider.WriteReport(entry, "D:\\dev\\temp\\output-ut.txt");
        }


        [Test]
        public void TestImportFromAD()
        {
            OUPrincipalMapping entry = new OUPrincipalMapping()
            {
                AdsPath = "LDAP://ad.monash.edu/OU=ADLDS,OU=Zone B,OU=Servers,OU=Tier 1,OU=Admin,DC=AD,DC=MONASH,DC=EDU",
                OUName = "OU=ADLDS,OU=Zone B,OU=Servers,OU=Tier 1,OU=Admin,DC=AD,DC=MONASH,DC=EDU"
            };

            provider.BuildAdminMapViaRpc(entry);

            provider.WriteReport(entry, "D:\\dev\\temp\\output.txt");
        }
    }
}
