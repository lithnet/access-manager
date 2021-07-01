using Lithnet.AccessManager.Agent.Configuration;
using Lithnet.AccessManager.Agent.Providers;
using Microsoft.Extensions.Options;
using Microsoft.Win32;
using Moq;
using NUnit.Framework;

namespace Lithnet.AccessManager.Agent.Test
{
    public class WindowsSettingsProviderTests
    {
        private RegistryKey policyPasswordKey;
        private RegistryKey policyAgentKey;
        private RegistryKey settingsKey;
        private WindowsSettingsProvider settings;
        private const string UnitTestKeyBase = "Software\\Lithnet\\UnitTest";
        private const string PolicyKeyBase = UnitTestKeyBase + "\\Policy\\Access Manager Agent";
        private const string PolicyPasswordKeyBase = PolicyKeyBase + "\\Password";
        private const string SettingsKeyBase = UnitTestKeyBase + "\\Access Manager Agent";

        [SetUp()]
        public void TestInitialize()
        {
            var agentOptions = new AgentOptions();
            var options = new Mock<IOptionsMonitor<AgentOptions>>();
            options.SetupGet(t => t.CurrentValue).Returns(agentOptions);

            var pathProviderMock = new Mock<IRegistryPathProvider>();
            pathProviderMock.SetupGet(t => t.PolicySettingsAgentPath).Returns($"HKEY_CURRENT_USER\\{PolicyKeyBase}");
            pathProviderMock.SetupGet(t => t.PolicySettingsPasswordPath).Returns($"HKEY_CURRENT_USER\\{PolicyPasswordKeyBase}");
            pathProviderMock.SetupGet(t => t.RegistrySettingsAgentPath).Returns($"HKEY_CURRENT_USER\\{SettingsKeyBase}");
            var pathProvider = pathProviderMock.Object;

            this.settings = new WindowsSettingsProvider(options.Object, pathProvider);

            Registry.CurrentUser.DeleteSubKeyTree(PolicyKeyBase, false);
            Registry.CurrentUser.DeleteSubKeyTree(PolicyPasswordKeyBase, false);
            Registry.CurrentUser.DeleteSubKeyTree(SettingsKeyBase, false);
            this.policyAgentKey = Registry.CurrentUser.CreateSubKey(PolicyKeyBase, true);
            this.policyPasswordKey = Registry.CurrentUser.CreateSubKey(PolicyPasswordKeyBase, true);
            this.settingsKey = Registry.CurrentUser.CreateSubKey(SettingsKeyBase, true);
        }

        [Test]
        public void AgentEnabled()
        {
            // Test default value
            Assert.AreEqual(false, this.settings.Enabled);

            // Test enabled
            policyAgentKey.SetValue("Enabled", 1);
            Assert.AreEqual(true, this.settings.Enabled);

            // Test disabled
            policyAgentKey.SetValue("Enabled", 0);
            Assert.AreEqual(false, this.settings.Enabled);
        }

        [Test]
        public void Interval()
        {
            Assert.AreEqual(60, this.settings.Interval);

            policyAgentKey.SetValue("Interval", 75);
            Assert.AreEqual(75, this.settings.Interval);
        }


        [Test]
        public void PasswordManagementEnabled()
        {
            // Test default value
            Assert.AreEqual(false, this.settings.PasswordManagementEnabled);

            // Test enabled
            policyPasswordKey.SetValue("Enabled", 1);
            Assert.AreEqual(true, this.settings.PasswordManagementEnabled);

            // Test disabled
            policyPasswordKey.SetValue("Enabled", 0);
            Assert.AreEqual(false, this.settings.PasswordManagementEnabled);
        }

        [Test]
        public void MaximumPasswordAge()
        {
            Assert.AreEqual(7, this.settings.MaximumPasswordAgeDays);

            policyPasswordKey.SetValue("MaximumPasswordAge", 55);
            Assert.AreEqual(55, this.settings.MaximumPasswordAgeDays);
        }

        [Test]
        public void PasswordCharacters()
        {
            Assert.AreEqual(null, this.settings.PasswordCharacters);

            policyPasswordKey.SetValue("PasswordCharacters", "abc123");
            Assert.AreEqual("abc123", this.settings.PasswordCharacters);
        }

        [Test]
        public void PasswordHistoryDaysToKeep()
        {
            Assert.AreEqual(30, this.settings.LithnetLocalAdminPasswordHistoryDaysToKeep);

            policyPasswordKey.SetValue("PasswordHistoryDaysToKeep", 99);
            Assert.AreEqual(99, this.settings.LithnetLocalAdminPasswordHistoryDaysToKeep);
        }

        [Test]
        public void PasswordLength()
        {
            Assert.AreEqual(16, this.settings.PasswordLength);

            policyPasswordKey.SetValue("PasswordLength", 88);
            Assert.AreEqual(88, this.settings.PasswordLength);
        }

        [Test]
        public void UseLower()
        {
            Assert.AreEqual(false, this.settings.UseLower);

            policyPasswordKey.SetValue("UseLower", 1);
            Assert.AreEqual(true, this.settings.UseLower);

            policyPasswordKey.SetValue("UseLower", 0);
            Assert.AreEqual(false, this.settings.UseLower);
        }

        [Test]
        public void UseNumeric()
        {
            Assert.AreEqual(false, this.settings.UseNumeric);

            policyPasswordKey.SetValue("UseNumeric", 1);
            Assert.AreEqual(true, this.settings.UseNumeric);

            policyPasswordKey.SetValue("UseNumeric", 0);
            Assert.AreEqual(false, this.settings.UseNumeric);
        }


        [Test]
        public void UseSymbol()
        {
            Assert.AreEqual(false, this.settings.UseSymbol);

            policyPasswordKey.SetValue("UseSymbol", 1);
            Assert.AreEqual(true, this.settings.UseSymbol);

            policyPasswordKey.SetValue("UseSymbol", 0);
            Assert.AreEqual(false, this.settings.UseSymbol);
        }

        [Test]
        public void UseUpper()
        {
            Assert.AreEqual(false, this.settings.UseUpper);

            policyPasswordKey.SetValue("UseUpper", 1);
            Assert.AreEqual(true, this.settings.UseUpper);

            policyPasswordKey.SetValue("UseUpper", 0);
            Assert.AreEqual(false, this.settings.UseUpper);
        }

        [Test]
        public void WriteToMsMcsAdmPasswordAttributes()
        {
            Assert.AreEqual(PasswordAttributeBehaviour.Ignore, this.settings.MsMcsAdmPwdAttributeBehaviour);

            policyPasswordKey.SetValue("MsMcsAdmPwdBehaviour", (int)PasswordAttributeBehaviour.Populate);
            Assert.AreEqual(PasswordAttributeBehaviour.Populate, this.settings.MsMcsAdmPwdAttributeBehaviour);

            policyPasswordKey.SetValue("MsMcsAdmPwdBehaviour", (int)PasswordAttributeBehaviour.Clear);
            Assert.AreEqual(PasswordAttributeBehaviour.Clear, this.settings.MsMcsAdmPwdAttributeBehaviour);

            policyPasswordKey.SetValue("MsMcsAdmPwdBehaviour", 0);
            Assert.AreEqual(PasswordAttributeBehaviour.Ignore, this.settings.MsMcsAdmPwdAttributeBehaviour);
        }
    }
}