using NUnit.Framework;
using System;
using System.Net;
using Lithnet.AccessManager.Agent.Providers;

namespace Lithnet.AccessManager.Agent.Windows.Test
{
    [TestFixture()]
    public class WindowsPlatformDataProviderTests
    {
        private WindowsPlatformDataProvider platformDataProvider;
        
        [SetUp()]
        public void TestInitialize()
        {
            this.platformDataProvider = new WindowsPlatformDataProvider();
        }

        [Test()]
        public void GetMachineNameTest()
        {
            StringAssert.AreEqualIgnoringCase(Environment.MachineName, this.platformDataProvider.GetMachineName());
        }

        [Test()]
        public void GetDnsNameTest()
        {
            StringAssert.AreEqualIgnoringCase(Dns.GetHostEntry("LocalHost").HostName, this.platformDataProvider.GetDnsName());
        }

        [Test()]
        public void GetOSNameTest()
        {
            StringAssert.AreEqualIgnoringCase("Windows", this.platformDataProvider.GetOSName());
        }

        [Test()]
        public void GetOSVersionTest()
        {
            StringAssert.AreEqualIgnoringCase(Environment.OSVersion.Version.ToString(), this.platformDataProvider.GetOSVersion());
        }

    }
}