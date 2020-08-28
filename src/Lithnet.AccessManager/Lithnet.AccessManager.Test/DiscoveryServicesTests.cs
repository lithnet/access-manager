using System.Security.Principal;
using NUnit.Framework;

namespace Lithnet.AccessManager.Test
{
    class DiscoveryServicesTests
    {
        private IDiscoveryServices discoveryServices;

        [SetUp()]
        public void TestInitialize()
        {
            this.discoveryServices = new DiscoveryServices();
        }

        [TestCase("idmdev1.local", "CN=Configuration,DC=IDMDEV1,DC=local")]
        [TestCase("subdev1.idmdev1.local", "CN=Configuration,DC=IDMDEV1,DC=local")]
        [TestCase("extdev1.local", "CN=Configuration,DC=extdev1,DC=local")]
        public void GetConfigurationNamingContext(string domain, string expected)
        {
            var de = this.discoveryServices.GetConfigurationNamingContext(domain);
            StringAssert.AreEqualIgnoringCase(expected, de.GetPropertyString("distinguishedName"));
        }


        [TestCase("idmdev1.local", "CN=Schema,CN=Configuration,DC=IDMDEV1,DC=local")]
        [TestCase("subdev1.idmdev1.local", "CN=Schema,CN=Configuration,DC=IDMDEV1,DC=local")]
        [TestCase("extdev1.local", "CN=Schema,CN=Configuration,DC=extdev1,DC=local")]
        public void GetSchemaNamingContext(string domain, string expected)
        {
            var de = this.discoveryServices.GetSchemaNamingContext(domain);
            StringAssert.AreEqualIgnoringCase(expected, de.GetPropertyString("distinguishedName"));
        }

        [TestCase("idmdev1.local", "idmd1ad1.idmdev1.local")]
        [TestCase("subdev1.idmdev1.local", "idmd1ad2.subdev1.idmdev1.local")]
        [TestCase("extdev1.local", "idmd1ad3.extdev1.local")]
        public void GetDc(string domain, string expected)
        {
            StringAssert.AreEqualIgnoringCase(expected, discoveryServices.GetDomainController(domain));
        }

        [TestCase("DC=IDMDEV1,DC=LOCAL", "idmdev1.local")]
        [TestCase("OU=Laps Testing,DC=IDMDEV1,DC=LOCAL", "idmdev1.local")]
        [TestCase("DC=EXTDEV1,DC=LOCAL", "extdev1.local")]
        [TestCase("OU=Laps Testing,DC=EXTDEV1,DC=LOCAL", "extdev1.local")]
        [TestCase("DC=SUBDEV1,DC=IDMDEV1,DC=LOCAL", "subdev1.idmdev1.local")]
        [TestCase("OU=Laps Testing,DC=SUBDEV1,DC=IDMDEV1,DC=LOCAL", "subdev1.idmdev1.local")]
        public void GetDomainNameDnsFromDn(string dn, string expected)
        {
            StringAssert.AreEqualIgnoringCase(expected, discoveryServices.GetDomainNameDns(dn));
        }

        [TestCase("DC=IDMDEV1,DC=LOCAL", "idmdev1.local")]
        [TestCase("OU=Laps Testing,DC=IDMDEV1,DC=LOCAL", "idmdev1.local")]
        [TestCase("DC=EXTDEV1,DC=LOCAL", "extdev1.local")]
        [TestCase("OU=Laps Testing,DC=EXTDEV1,DC=LOCAL", "extdev1.local")]
        [TestCase("DC=SUBDEV1,DC=IDMDEV1,DC=LOCAL", "idmdev1.local")]
        [TestCase("OU=Laps Testing,DC=SUBDEV1,DC=IDMDEV1,DC=LOCAL", "idmdev1.local")]
        public void GetForestNameDnsFromDn(string dn, string expected)
        {
            StringAssert.AreEqualIgnoringCase(expected, discoveryServices.GetForestNameDns(dn));
        }

        [TestCase("IDMDEV1\\user1", "idmdev1")]
        [TestCase("extdev1\\user1", "extdev1")]
        [TestCase("subdev1\\user1", "subdev1")]
        public void GetDomainNameNetBiosFromSid(string referenceAccount, string expected)
        {
            ActiveDirectory ad = new ActiveDirectory(discoveryServices);
            SecurityIdentifier sid = ad.GetUser(referenceAccount).Sid;
            StringAssert.AreEqualIgnoringCase(expected, discoveryServices.GetDomainNameNetBios(sid));
        }

        [TestCase("IDMDEV1\\user1", "idmdev1.local")]
        [TestCase("extdev1\\user1", "extdev1.local")]
        [TestCase("subdev1\\user1", "subdev1.idmdev1.local")]
        public void GetDomainNameDnsFromSid(string referenceAccount, string expected)
        {
            ActiveDirectory ad = new ActiveDirectory(discoveryServices);
            SecurityIdentifier sid = ad.GetUser(referenceAccount).Sid;
            StringAssert.AreEqualIgnoringCase(expected, discoveryServices.GetDomainNameDns(sid));
        }

        [TestCase("DC=IDMDEV1,DC=LOCAL", "idmd1ad1.idmdev1.local")]
        [TestCase("OU=Laps Testing,DC=IDMDEV1,DC=LOCAL", "idmd1ad1.idmdev1.local")]
        [TestCase("DC=EXTDEV1,DC=LOCAL", "idmd1ad3.extdev1.local")]
        [TestCase("OU=Laps Testing,DC=EXTDEV1,DC=LOCAL", "idmd1ad3.extdev1.local")]
        [TestCase("DC=SUBDEV1,DC=IDMDEV1,DC=LOCAL", "idmd1ad2.subdev1.idmdev1.local")]
        [TestCase("OU=Laps Testing,DC=SUBDEV1,DC=IDMDEV1,DC=LOCAL", "idmd1ad2.subdev1.idmdev1.local")]
        public void GetDomainControllerFromDN(string dn, string expected)
        {
            StringAssert.AreEqualIgnoringCase(expected, discoveryServices.GetDomainControllerFromDN(dn));
        }
    }
}