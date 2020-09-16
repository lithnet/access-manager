using System;
using System.Collections.Generic;
using System.Text;
using Lithnet.AccessManager.Server.Configuration;
using Lithnet.AccessManager.Server.Providers;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace Lithnet.AccessManager.Server.Test
{
    class JitAuthorizationRuleImportProviderTests
    {
        private IDirectory directory;

        private IDiscoveryServices discoveryServices;

        private JitAuthorizationRuleImportProvider provider;

        private ILogger<JitAuthorizationRuleImportProvider> logger;

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
            logger = Global.LogFactory.CreateLogger<JitAuthorizationRuleImportProvider>();
            provider = new JitAuthorizationRuleImportProvider(logger, localSam, directory);
        }

        [Test]
        public void Test()
        {
            OUEntry entry = new OUEntry()
            {
                AdsPath = "LDAP://DC=idmdev1,DC=local",
                OUName = "DC=idmdev1,DC=local"
            };

            provider.GetOUEntries(entry);

            int x = 0;

        }
    }
}
