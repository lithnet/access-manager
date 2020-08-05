using Microsoft.Win32;
using NUnit.Framework;

namespace Lithnet.AccessManager.Agent.Test
{
    public class AgentRegistryTests
    {
        private const string policyKeyName = "SOFTWARE\\Lithnet\\UnitTest\\Access Manager Agent";

        private RegistryKey policyKey;

        private AgentRegistrySettings registrySettings;

        [SetUp()]
        public void TestInitialize()
        {
            Registry.CurrentUser.DeleteSubKeyTree(policyKeyName, false);

            this.policyKey = Registry.CurrentUser.CreateSubKey(policyKeyName, true);
            this.registrySettings = new AgentRegistrySettings(policyKey, policyKey);
        }

        [Test]
        public void Enabled()
        {
            // Test default value
            Assert.AreEqual(false, this.registrySettings.Enabled);

            // Test enabled
            policyKey.SetValue("Enabled", 1);
            Assert.AreEqual(true, this.registrySettings.Enabled);

            // Test disabled
            policyKey.SetValue("Enabled", 0);
            Assert.AreEqual(false, this.registrySettings.Enabled);
        }

        [Test]
        public void Interval()
        {
            Assert.AreEqual(60, this.registrySettings.Interval);

            policyKey.SetValue("Interval", 75);
            Assert.AreEqual(75, this.registrySettings.Interval);
        }
    }
}