using Lithnet.AccessManager.Agent.Providers;
using Microsoft.Win32;
using Moq;
using NUnit.Framework;
using System;

namespace Lithnet.AccessManager.Agent.Test
{
    public class WindowsAgentSettingsProviderTests
    {
        private RegistryKey policyPasswordKey;
        private RegistryKey policyAgentKey;
        private RegistryKey settingsKey;
        private RegistryKey stateKey;
        private WindowsAgentSettingsProvider agentSettings;
        private ActiveDirectoryLapsSettingsProvider lapsSettings;
        private const string UnitTestKeyBase = "Software\\Lithnet\\UnitTest";
        private const string PolicyKeyBase = UnitTestKeyBase + "\\Policy\\Access Manager Agent";
        private const string PolicyPasswordKeyBase = PolicyKeyBase + "\\Password";
        private const string SettingsKeyBase = UnitTestKeyBase + "\\Access Manager Agent";
        private const string StateKeyBase = SettingsKeyBase + "\\State";

        [SetUp()]
        public void TestInitialize()
        {
            var pathProviderMock = new Mock<IRegistryPathProvider>();
            pathProviderMock.SetupGet(t => t.PolicyAgentPath).Returns($"HKEY_CURRENT_USER\\{PolicyKeyBase}");
            pathProviderMock.SetupGet(t => t.PolicyPasswordPath).Returns($"HKEY_CURRENT_USER\\{PolicyPasswordKeyBase}");
            pathProviderMock.SetupGet(t => t.SettingsAgentPath).Returns($"HKEY_CURRENT_USER\\{SettingsKeyBase}");
            pathProviderMock.SetupGet(t => t.StatePath).Returns($"HKEY_CURRENT_USER\\{StateKeyBase}");
            var pathProvider = pathProviderMock.Object;

            this.agentSettings = new WindowsAgentSettingsProvider(pathProvider);
            this.lapsSettings = new ActiveDirectoryLapsSettingsProvider(pathProvider);

            Registry.CurrentUser.DeleteSubKeyTree(PolicyKeyBase, false);
            Registry.CurrentUser.DeleteSubKeyTree(PolicyPasswordKeyBase, false);
            Registry.CurrentUser.DeleteSubKeyTree(SettingsKeyBase, false);
            Registry.CurrentUser.DeleteSubKeyTree(StateKeyBase, false);
            this.policyAgentKey = Registry.CurrentUser.CreateSubKey(PolicyKeyBase, true);
            this.policyPasswordKey = Registry.CurrentUser.CreateSubKey(PolicyPasswordKeyBase, true);
            this.settingsKey = Registry.CurrentUser.CreateSubKey(SettingsKeyBase, true);
            this.stateKey = Registry.CurrentUser.CreateSubKey(StateKeyBase, true);
        }

        [Test]
        public void AgentEnabled()
        {
            this.TestAgentValue("Enabled", false, () => this.agentSettings.Enabled);
        }

        [Test]
        public void Interval()
        {
            this.TestAgentValue("Interval", 60, () => this.agentSettings.Interval);
        }

        [Test]
        public void AmsServerManagementEnabled()
        {
            this.TestAgentValue("AmsServerManagementEnabled", false, () => this.agentSettings.AmsServerManagementEnabled);
        }

        [Test]
        public void AmsPasswordStorageEnabled()
        {
            this.TestAgentValue("AmsPasswordStorageEnabled", false, () => this.agentSettings.AmsPasswordStorageEnabled);
        }

        [Test]
        public void AuthenticationMode()
        {
            this.TestAgentValue("AuthenticationMode", 0, () => (int)this.agentSettings.AuthenticationMode);
        }

        [Test]
        public void Server()
        {
            this.TestAgentValue("Server", null, () => this.agentSettings.Server);
        }

        [Test]
        public void AzureAdTenantId()
        {
            this.TestAgentValue("AzureAdTenantId", null, () => this.agentSettings.AzureAdTenantId);
        }

        [Test]
        public void CheckInIntervalHours()
        {
            this.TestAgentValue("CheckInIntervalHours", 24, () => this.agentSettings.CheckInIntervalHours);
        }

        [Test]
        public void RegisterSecondaryCredentialsForAadj()
        {
            this.TestAgentValue("RegisterSecondaryCredentialsForAadj", false, () => this.agentSettings.RegisterSecondaryCredentialsForAadj);
        }

        [Test]
        public void RegisterSecondaryCredentialsForAadr()
        {
            this.TestAgentValue("RegisterSecondaryCredentialsForAadr", true, () => this.agentSettings.RegisterSecondaryCredentialsForAadr);
        }

        [Test]
        public void LapsSettingsEnabled()
        {
            this.TestPasswordPolicyValue("Enabled", false, () => this.lapsSettings.Enabled);
        }

        [Test]
        public void PasswordLength()
        {
            this.TestPasswordPolicyValue("PasswordLength", 16, () => this.lapsSettings.PasswordLength);
        }

        [Test]
        public void PasswordCharacters()
        {
            this.TestPasswordPolicyValue("PasswordCharacters", null, () => this.lapsSettings.PasswordCharacters);
        }

        [Test]
        public void UseUpper()
        {
            this.TestPasswordPolicyValue("UseUpper", true, () => this.lapsSettings.UseUpper);
        }

        [Test]
        public void UseLower()
        {
            this.TestPasswordPolicyValue("UseLower", true, () => this.lapsSettings.UseLower);
        }

        [Test]
        public void UseSymbol()
        {
            this.TestPasswordPolicyValue("UseSymbol", false, () => this.lapsSettings.UseSymbol);
        }

        [Test]
        public void UseNumeric()
        {
            this.TestPasswordPolicyValue("UseNumeric", true, () => this.lapsSettings.UseNumeric);
        }

        [Test]
        public void PasswordHistoryDaysToKeep()
        {
            this.TestPasswordPolicyValue("PasswordHistoryDaysToKeep", 0, () => this.lapsSettings.PasswordHistoryDaysToKeep);
        }

        [Test]
        public void MsMcsAdmPwdBehaviour()
        {
            this.TestPasswordPolicyValue("MsMcsAdmPwdBehaviour", 0, () => (int)this.lapsSettings.MsMcsAdmPwdBehaviour);
        }

        [Test]
        public void MaximumPasswordAge()
        {
            this.TestPasswordPolicyValue("MaximumPasswordAge", 7, () => this.lapsSettings.MaximumPasswordAgeDays);
        }

        [Test]
        public void RegistrationKey()
        {
            this.TestStateValue("RegistrationKey", null, () => this.agentSettings.RegistrationKey, (x) => this.agentSettings.RegistrationKey = x);
        }

        [Test]
        public void ClientId()
        {
            this.TestStateValue("ClientId", null, () => this.agentSettings.ClientId, (x) => this.agentSettings.ClientId = x);
        }

        [Test]
        public void CheckRegistrationUrl()
        {
            this.TestStateValue("CheckRegistrationUrl", null, () => this.agentSettings.CheckRegistrationUrl, (x) => this.agentSettings.CheckRegistrationUrl = x);
        }

        [Test]
        public void AuthCertificate()
        {
            this.TestStateValue("AuthCertificate", null, () => this.agentSettings.AuthCertificate, (x) => this.agentSettings.AuthCertificate = x);
        }

        [Test]
        public void LastCheckIn()
        {
            this.TestStateValue("LastCheckIn", (int)0, () => (int)this.agentSettings.LastCheckIn.Ticks, (int x) => this.agentSettings.LastCheckIn = new DateTime((long)x));
        }

        [Test]
        public void HasRegisteredSecondaryCredentials()
        {
            this.TestStateValue("HasRegisteredSecondaryCredentials", false, () => this.agentSettings.HasRegisteredSecondaryCredentials, (x) => this.agentSettings.HasRegisteredSecondaryCredentials = x);
        }

        [Test]
        public void RegistrationState()
        {
            this.TestStateValue("RegistrationState", (int)0, () => (int)this.agentSettings.RegistrationState, (int x) => this.agentSettings.RegistrationState = (RegistrationState)x);
        }

        private void TestAgentValue(string name, bool defaultValue, Func<bool> getSetting)
        {
            // Test default value
            Assert.AreEqual(defaultValue, getSetting());

            // Test enabled
            policyAgentKey.SetValue(name, 1);
            Assert.AreEqual(true, getSetting());

            // Test disabled
            policyAgentKey.SetValue(name, 0);
            Assert.AreEqual(false, getSetting());

            // Make sure settings are override by policy
            settingsKey.SetValue(name, 1);
            Assert.AreEqual(false, getSetting());

            // Make sure setting is read when policy is not present
            policyAgentKey.DeleteValue(name);
            Assert.AreEqual(true, getSetting());
        }

        private void TestAgentValue(string name, int defaultValue, Func<int> getSetting)
        {
            // Test default value
            Assert.AreEqual(defaultValue, getSetting());

            // Test enabled
            policyAgentKey.SetValue(name, 1);
            Assert.AreEqual(1, getSetting());

            // Test disabled
            policyAgentKey.SetValue(name, 0);
            Assert.AreEqual(0, getSetting());

            // Make sure settings are override by policy
            settingsKey.SetValue(name, 1);
            Assert.AreEqual(0, getSetting());

            // Make sure setting is read when policy is not present
            policyAgentKey.DeleteValue(name);
            Assert.AreEqual(1, getSetting());
        }


        private void TestAgentValue(string name, string defaultValue, Func<string> getSetting)
        {
            // Test default value
            Assert.AreEqual(defaultValue, getSetting());

            // Test set value
            policyAgentKey.SetValue(name, "abc");
            Assert.AreEqual("abc", getSetting());

            // Make sure settings are override by policy
            settingsKey.SetValue(name, "def");
            Assert.AreEqual("abc", getSetting());

            // Make sure setting is read when policy is not present
            policyAgentKey.DeleteValue(name);
            Assert.AreEqual("def", getSetting());
        }

        private void TestPasswordPolicyValue(string name, bool defaultValue, Func<bool> getSetting)
        {
            Assert.AreEqual(defaultValue, getSetting());

            policyPasswordKey.SetValue(name, 1);
            Assert.AreEqual(true, getSetting());

            policyPasswordKey.SetValue(name, 0);
            Assert.AreEqual(false, getSetting());
        }

        private void TestPasswordPolicyValue(string name, int defaultValue, Func<int> getSetting)
        {
            Assert.AreEqual(defaultValue, getSetting());

            policyPasswordKey.SetValue(name, 1);
            Assert.AreEqual(1, getSetting());

            policyPasswordKey.SetValue(name, 0);
            Assert.AreEqual(0, getSetting());
        }

        private void TestPasswordPolicyValue(string name, string defaultValue, Func<string> getSetting)
        {
            // Test default value
            Assert.AreEqual(defaultValue, getSetting());

            // Test enabled
            policyPasswordKey.SetValue(name, "abc");
            Assert.AreEqual("abc", getSetting());
        }

        private void TestStateValue(string name, bool defaultValue, Func<bool> getSetting, Action<bool> setSetting)
        {
            Assert.AreEqual(defaultValue, getSetting());

            stateKey.SetValue(name, 1);
            Assert.AreEqual(true, getSetting());

            stateKey.SetValue(name, 0);
            Assert.AreEqual(false, getSetting());

            setSetting(true);
            Assert.AreEqual(true, getSetting());

            setSetting(false);
            Assert.AreEqual(false, getSetting());
        }

        private void TestStateValue(string name, int defaultValue, Func<int> getSetting, Action<int> setSetting)
        {
            Assert.AreEqual(defaultValue, getSetting());

            stateKey.SetValue(name, 1);
            Assert.AreEqual(1, getSetting());

            stateKey.SetValue(name, 0);
            Assert.AreEqual(0, getSetting());

            setSetting(99);
            Assert.AreEqual(99, getSetting());
        }

        private void TestStateValue(string name, string defaultValue, Func<string> getSetting, Action<string> setSetting)
        {
            Assert.AreEqual(defaultValue, getSetting());

            stateKey.SetValue(name, "abc");
            Assert.AreEqual("abc", getSetting());

            setSetting("abcdefg");
            Assert.AreEqual("abcdefg", getSetting());
        }
    }
}