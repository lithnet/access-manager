using System;
using System.Diagnostics.Contracts;
using System.Security.Principal;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Vanara.PInvoke;
using C = Lithnet.AccessManager.Test.TestEnvironmentConstants;

namespace Lithnet.AccessManager.Test
{
    class DiscoveryServicesTests
    {
        private DiscoveryServices discoveryServices;

        [SetUp()]
        public void TestInitialize()
        {
            this.discoveryServices = new DiscoveryServices(Global.LogFactory.CreateLogger<DiscoveryServices>());
        }

        [TestCase(C.DevLocal, "CN=Configuration," + C.DevDN)]
        [TestCase(C.SubDevLocal, "CN=Configuration," + C.DevDN)]
        [TestCase(C.ExtDevLocal, "CN=Configuration," + C.ExtDevDN)]
        public void GetConfigurationNamingContext(string domain, string expected)
        {
            var de = this.discoveryServices.GetConfigurationNamingContext(domain);
            StringAssert.AreEqualIgnoringCase(expected, de.GetPropertyString("distinguishedName"));
        }

        [TestCase(C.DevLocal, "CN=Schema,CN=Configuration," + C.DevDN)]
        [TestCase(C.SubDevLocal, "CN=Schema,CN=Configuration," + C.DevDN)]
        [TestCase(C.ExtDevLocal, "CN=Schema,CN=Configuration," + C.ExtDevDN)]
        public void GetSchemaNamingContext(string domain, string expected)
        {
            var de = this.discoveryServices.GetSchemaNamingContext(domain);
            StringAssert.AreEqualIgnoringCase(expected, de.GetPropertyString("distinguishedName"));
        }

        [TestCase(C.DevLocal, C.DevDc)]
        [TestCase(C.SubDevLocal, C.SubDevDc)]
        [TestCase(C.ExtDevLocal, C.ExtDevDc)]
        public void GetDc(string domain, string expected)
        {
            StringAssert.AreEqualIgnoringCase(expected, discoveryServices.GetDomainController(domain));
        }

        [TestCase(C.DevDN, C.DevLocal)]
        [TestCase(C.AmsTesting_DevDN, C.DevLocal)]
        [TestCase(C.ExtDevDN, C.ExtDevLocal)]
        [TestCase(C.AmsTesting_ExtDevDN, C.ExtDevLocal)]
        [TestCase(C.SubDevDN, C.SubDevLocal)]
        [TestCase(C.AmsTesting_SubDevDN, C.SubDevLocal)]
        public void GetDomainNameDnsFromDn(string dn, string expected)
        {
            StringAssert.AreEqualIgnoringCase(expected, discoveryServices.GetDomainNameDns(dn));
        }

        [TestCase(C.DevDN, C.DevLocal)]
        [TestCase(C.AmsTesting_DevDN, C.DevLocal)]
        [TestCase(C.ExtDevDN, C.ExtDevLocal)]
        [TestCase(C.AmsTesting_ExtDevDN, C.ExtDevLocal)]
        [TestCase(C.SubDevDN, C.DevLocal)]
        [TestCase(C.AmsTesting_SubDevDN, C.DevLocal)]
        public void GetForestNameDnsFromDn(string dn, string expected)
        {
            StringAssert.AreEqualIgnoringCase(expected, discoveryServices.GetForestNameDns(dn));
        }

        [TestCase(C.DEV_User1, C.Dev)]
        [TestCase(C.EXTDEV_User1, C.ExtDev)]
        [TestCase(C.SUBDEV_User1, C.SubDev)]
        public void GetDomainNameNetBiosFromSid(string referenceAccount, string expected)
        {
            ActiveDirectory ad = new ActiveDirectory(discoveryServices);
            SecurityIdentifier sid = ad.GetUser(referenceAccount).Sid;
            StringAssert.AreEqualIgnoringCase(expected, discoveryServices.GetDomainNameNetBios(sid));
        }

        [TestCase(C.DEV_User1, C.DevLocal)]
        [TestCase(C.EXTDEV_User1, C.ExtDevLocal)]
        [TestCase(C.SUBDEV_User1, C.SubDevLocal)]
        public void GetDomainNameDnsFromSid(string referenceAccount, string expected)
        {
            ActiveDirectory ad = new ActiveDirectory(discoveryServices);
            SecurityIdentifier sid = ad.GetUser(referenceAccount).Sid;
            StringAssert.AreEqualIgnoringCase(expected, discoveryServices.GetDomainNameDns(sid));
        }

        [TestCase(C.DevDN, C.DevDc)]
        [TestCase(C.AmsTesting_DevDN, C.DevDc)]
        [TestCase(C.ExtDevDN, C.ExtDevDc)]
        [TestCase(C.AmsTesting_ExtDevDN, C.ExtDevDc)]
        [TestCase(C.SubDevDN, C.SubDevDc)]
        [TestCase(C.AmsTesting_SubDevDN, C.SubDevDc)]
        public void GetDomainControllerFromDN(string dn, string expected)
        {
            StringAssert.AreEqualIgnoringCase(expected, discoveryServices.GetDomainControllerFromDN(dn));
        }

        [TestCase(C.DevDc, C.DevLocal, C.DevDc)]
        [TestCase("PC1." + C.DevLocal, C.DevLocal, C.DevDc)]
        [TestCase(C.SubDevDc, C.SubDevLocal, C.SubDevDc)]
        [TestCase(C.ExtDevDc, C.ExtDevLocal, C.ExtDevDc)]
        public void GetDomainControllerForComputer(string computerName, string domain, string expected)
        {
            StringAssert.AreEqualIgnoringCase(expected, discoveryServices.GetDomainController(computerName, domain, Interop.DsGetDcNameFlags.DS_DIRECTORY_SERVICE_REQUIRED));
        }

        [TestCase(C.DevDc, C.DevDefaultSite)]
        [TestCase(C.SubDevDc, C.SubDevDefaultSite)]
        [TestCase(C.ExtDevDc, C.ExtDevDefaultSite)]
        public void GetSiteForComputer(string computer, string expected)
        {
            StringAssert.AreEqualIgnoringCase(expected, discoveryServices.GetComputerSiteNameRpc(computer));
        }
    }
}