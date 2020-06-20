using System;
using Lithnet.AccessManager.Agent;
using Microsoft.Win32;
using NUnit.Framework;

namespace Lithnet.AccessManager.Agent.Test
{
    public class LapsRegistryTests
    {
        private const string policyKeyName = "SOFTWARE\\Lithnet\\UnitTest\\AccessManager\\Agent\\Laps";

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
        public void LapsEnabled()
        {
            // Test default value
            Assert.AreEqual(false, this.registrySettings.LapsEnabled);

            // Test enabled
            policyKey.SetValue("LapsEnabled", 1);
            Assert.AreEqual(true, this.registrySettings.LapsEnabled);

            // Test disabled
            policyKey.SetValue("LapsEnabled", 0);
            Assert.AreEqual(false, this.registrySettings.LapsEnabled);
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
        public void SigningCertThumbprint()
        {
            Assert.AreEqual(null, this.registrySettings.SigningCertThumbprint);

            policyKey.SetValue("SigningCertThumbprint", "ABCDEFG");
            Assert.AreEqual("ABCDEFG", this.registrySettings.SigningCertThumbprint);
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
        public void UseReadibilitySeparator()
        {
            Assert.AreEqual(false, this.registrySettings.UseReadibilitySeparator);

            policyKey.SetValue("UseReadibilitySeparator", 1);
            Assert.AreEqual(true, this.registrySettings.UseReadibilitySeparator);

            policyKey.SetValue("UseReadibilitySeparator", 0);
            Assert.AreEqual(false, this.registrySettings.UseReadibilitySeparator);
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
        public void WriteToAppData()
        {
            Assert.AreEqual(false, this.registrySettings.WriteToAppData);
            
            policyKey.SetValue("WriteToAppData", 1);
            Assert.AreEqual(true, this.registrySettings.WriteToAppData);

            policyKey.SetValue("WriteToAppData", 0);
            Assert.AreEqual(false, this.registrySettings.WriteToAppData);
        }

        [Test]
        public void WriteToMsMcsAdmPasswordAttributes()
        {
            Assert.AreEqual(false, this.registrySettings.WriteToMsMcsAdmPasswordAttributes);

            policyKey.SetValue("WriteToMsMcsAdmPasswordAttributes", 1);
            Assert.AreEqual(true, this.registrySettings.WriteToMsMcsAdmPasswordAttributes);

            policyKey.SetValue("WriteToMsMcsAdmPasswordAttributes", 0);
            Assert.AreEqual(false, this.registrySettings.WriteToMsMcsAdmPasswordAttributes);
        }
    }
}