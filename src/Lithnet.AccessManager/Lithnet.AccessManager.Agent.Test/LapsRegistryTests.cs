using Microsoft.Win32;
using NUnit.Framework;

namespace Lithnet.AccessManager.Agent.Test
{
    public class LapsRegistryTests
    {
        private const string policyKeyName = "SOFTWARE\\Lithnet\\UnitTest\\Access Manager Agent\\Laps";

        private RegistryKey policyKey;

        private LapsRegistrySettings registrySettings;

        [SetUp()]
        public void TestInitialize()
        {
            Registry.CurrentUser.DeleteSubKey(policyKeyName, false);

            this.policyKey = Registry.CurrentUser.CreateSubKey(policyKeyName, true);
            this.registrySettings = new LapsRegistrySettings(policyKey, policyKey);
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
        public void MaximumPasswordAge()
        {
            Assert.AreEqual(14, this.registrySettings.MaximumPasswordAge);

            policyKey.SetValue("MaximumPasswordAge", 55);
            Assert.AreEqual(55, this.registrySettings.MaximumPasswordAge);
        }

        [Test]
        public void PasswordCharacters()
        {
            Assert.AreEqual(null, this.registrySettings.PasswordCharacters); 

            policyKey.SetValue("PasswordCharacters", "abc123");
            Assert.AreEqual("abc123", this.registrySettings.PasswordCharacters);
        }

        [Test]
        public void PasswordHistoryDaysToKeep()
        {
            Assert.AreEqual(0, this.registrySettings.PasswordHistoryDaysToKeep);

            policyKey.SetValue("PasswordHistoryDaysToKeep", 99);
            Assert.AreEqual(99, this.registrySettings.PasswordHistoryDaysToKeep);
        }

        [Test]
        public void PasswordLength()
        {
            Assert.AreEqual(16, this.registrySettings.PasswordLength);

            policyKey.SetValue("PasswordLength", 88);
            Assert.AreEqual(88, this.registrySettings.PasswordLength);
        }

        [Test]
        public void ReadabilitySeparator()
        {
            Assert.AreEqual("-", this.registrySettings.ReadabilitySeparator);

            policyKey.SetValue("ReadabilitySeparator", "%");
            Assert.AreEqual("%", this.registrySettings.ReadabilitySeparator);
        }

        [Test]
        public void ReadabilitySeparatorInterval()
        {
            Assert.AreEqual(4, this.registrySettings.ReadabilitySeparatorInterval);

            policyKey.SetValue("ReadabilitySeparatorInterval", 5);
            Assert.AreEqual(5, this.registrySettings.ReadabilitySeparatorInterval);
        }

        [Test]
        public void UseLower()
        {
            Assert.AreEqual(false, this.registrySettings.UseLower);

            policyKey.SetValue("UseLower", 1);
            Assert.AreEqual(true, this.registrySettings.UseLower);

            policyKey.SetValue("UseLower", 0);
            Assert.AreEqual(false, this.registrySettings.UseLower);
        }

        [Test]
        public void UseNumeric()
        {
            Assert.AreEqual(false, this.registrySettings.UseNumeric);

            policyKey.SetValue("UseNumeric", 1);
            Assert.AreEqual(true, this.registrySettings.UseNumeric);

            policyKey.SetValue("UseNumeric", 0);
            Assert.AreEqual(false, this.registrySettings.UseNumeric);
        }

        [Test]
        public void UseReadabilitySeparator()
        {
            Assert.AreEqual(false, this.registrySettings.UseReadabilitySeparator);

            policyKey.SetValue("UseReadabilitySeparator", 1);
            Assert.AreEqual(true, this.registrySettings.UseReadabilitySeparator);

            policyKey.SetValue("UseReadabilitySeparator", 0);
            Assert.AreEqual(false, this.registrySettings.UseReadabilitySeparator);
        }

        [Test]
        public void UseSymbol()
        {
            Assert.AreEqual(false, this.registrySettings.UseSymbol);

            policyKey.SetValue("UseSymbol", 1);
            Assert.AreEqual(true, this.registrySettings.UseSymbol);

            policyKey.SetValue("UseSymbol", 0);
            Assert.AreEqual(false, this.registrySettings.UseSymbol);
        }

        [Test]
        public void UseUpper()
        {
            Assert.AreEqual(false, this.registrySettings.UseUpper);

            policyKey.SetValue("UseUpper", 1);
            Assert.AreEqual(true, this.registrySettings.UseUpper);

            policyKey.SetValue("UseUpper", 0);
            Assert.AreEqual(false, this.registrySettings.UseUpper);
        }

        [Test]
        public void WriteToMsMcsAdmPasswordAttributes()
        {
            Assert.AreEqual(MsMcsAdmPwdBehaviour.Ignore, this.registrySettings.MsMcsAdmPwdBehaviour);

            policyKey.SetValue("MsMcsAdmPwdBehaviour", (int)MsMcsAdmPwdBehaviour.Populate);
            Assert.AreEqual(MsMcsAdmPwdBehaviour.Populate, this.registrySettings.MsMcsAdmPwdBehaviour);

            policyKey.SetValue("MsMcsAdmPwdBehaviour", (int)MsMcsAdmPwdBehaviour.Clear);
            Assert.AreEqual(MsMcsAdmPwdBehaviour.Clear, this.registrySettings.MsMcsAdmPwdBehaviour);

            policyKey.SetValue("MsMcsAdmPwdBehaviour", 0);
            Assert.AreEqual(MsMcsAdmPwdBehaviour.Ignore, this.registrySettings.MsMcsAdmPwdBehaviour);
        }
    }
}