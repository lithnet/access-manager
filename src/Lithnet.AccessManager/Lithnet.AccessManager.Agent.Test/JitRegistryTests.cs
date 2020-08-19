using Microsoft.Win32;
using NUnit.Framework;

namespace Lithnet.AccessManager.Agent.Test
{
    public class JitRegistryTests
    {
        private const string policyKeyName = "SOFTWARE\\Lithnet\\UnitTest\\Access Manager Agent\\Jit";

        private RegistryKey policyKey;

        private RegistrySettingsJit registrySettings;

        [SetUp()]
        public void TestInitialize()
        {
            Registry.CurrentUser.DeleteSubKey(policyKeyName, false);

            this.policyKey = Registry.CurrentUser.CreateSubKey(policyKeyName, true);
            this.registrySettings = new RegistrySettingsJit($"HKEY_CURRENT_USER\\{policyKeyName}");
        }

        [Test]
        public void JitEnabled()
        {
            // Test default value
            Assert.AreEqual(false, this.registrySettings.JitEnabled);

            // Test enabled
            policyKey.SetValue("Enabled", 1);
            Assert.AreEqual(true, this.registrySettings.JitEnabled);

            // Test disabled
            policyKey.SetValue("Enabled", 0);
            Assert.AreEqual(false, this.registrySettings.JitEnabled);
        }

        [Test]
        public void AllowedAdmins()
        {
            CollectionAssert.IsEmpty(this.registrySettings.AllowedAdmins);

            policyKey.SetValue("AllowedAdmins", new[] { "user1", "user2" });
            CollectionAssert.AreEqual(new[] { "user1", "user2" }, this.registrySettings.AllowedAdmins);
        }

        [Test]
        public void RestrictAdmins()
        {
            Assert.AreEqual(false, this.registrySettings.RestrictAdmins);

            policyKey.SetValue("RestrictAdmins", 1);
            Assert.AreEqual(true, this.registrySettings.RestrictAdmins);

            policyKey.SetValue("RestrictAdmins", 0);
            Assert.AreEqual(false, this.registrySettings.RestrictAdmins);
        }

        [Test]
        public void JitGroup()
        {
            Assert.AreEqual(null, this.registrySettings.JitGroup);

            policyKey.SetValue("JitGroup", "TestGroup");
            Assert.AreEqual("TestGroup", this.registrySettings.JitGroup);
        }
    }
}