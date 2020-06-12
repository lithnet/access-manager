using Moq;
using System;
using NLog;
using NUnit.Framework;
using AD = Lithnet.Laps.Web.ActiveDirectory.ActiveDirectory;
using Lithnet.Laps.Web.Models;
using Lithnet.Laps.Web.ActiveDirectory;
using System.Security.Principal;

namespace Lithnet.Laps.Web.Test
{
    /// <summary>
    /// These test cases require two computers in each domain called 'PC1' and PC2' located in an OU called OU=Computers,OU=LAPS Testing at the root of the domain.
    /// This computers should have a LAPS password with the value "{domain}\{ComputerName} Password" (eg "IDMDEV1\PC1 Password") 
    /// The computers named PC1 should have an expiry date of 9999999999999
    /// 
    /// These test cases also depend on the users and group structure defined in the AceTests class
    /// </summary>
    class DirectoryTests
    {
        private Mock<ILogger> dummyLogger;
        private AD directory;

        [SetUp()]
        public void TestInitialize()
        {
            dummyLogger = new Mock<ILogger>();
            this.directory = new AD();
        }

        // THIS domain user found in THIS domain group
        [TestCase("IDMDEV1\\user1", "IDMDEV1\\G-DL-1"), TestCase("IDMDEV1\\user1", "IDMDEV1\\G-GG-1"), TestCase("IDMDEV1\\user1", "IDMDEV1\\G-UG-1")]
        [TestCase("IDMDEV1\\user2", "IDMDEV1\\G-UG-2"), TestCase("IDMDEV1\\user2", "IDMDEV1\\G-GG-2"), TestCase("IDMDEV1\\user2", "IDMDEV1\\G-UG-2")]
        [TestCase("IDMDEV1\\user3", "IDMDEV1\\G-DL-3"), TestCase("IDMDEV1\\user3", "IDMDEV1\\G-GG-3"), TestCase("IDMDEV1\\user3", "IDMDEV1\\G-UG-3")]

        // THIS domain user found in CHILD domain group
        [TestCase("IDMDEV1\\user1", "SUBDEV1\\G-DL-1"), TestCase("IDMDEV1\\user1", "SUBDEV1\\G-UG-1")]
        [TestCase("IDMDEV1\\user2", "SUBDEV1\\G-DL-2"), TestCase("IDMDEV1\\user2", "SUBDEV1\\G-UG-2")]
        [TestCase("IDMDEV1\\user3", "SUBDEV1\\G-DL-3"), TestCase("IDMDEV1\\user3", "SUBDEV1\\G-UG-3")]

        // User principals match themselves
        [TestCase("IDMDEV1\\user1", "IDMDEV1\\user1"), TestCase("SUBDEV1\\user1", "SUBDEV1\\user1")]

        // CHILD domain user found in CHILD domain group
        [TestCase("SUBDEV1\\user1", "SUBDEV1\\G-DL-1"), TestCase("SUBDEV1\\user1", "SUBDEV1\\G-GG-1"), TestCase("SUBDEV1\\user1", "SUBDEV1\\G-UG-1")]
        [TestCase("SUBDEV1\\user2", "SUBDEV1\\G-DL-2"), TestCase("SUBDEV1\\user2", "SUBDEV1\\G-GG-2"), TestCase("SUBDEV1\\user2", "SUBDEV1\\G-UG-2")]
        [TestCase("SUBDEV1\\user3", "SUBDEV1\\G-DL-3"), TestCase("SUBDEV1\\user3", "SUBDEV1\\G-GG-3"), TestCase("SUBDEV1\\user3", "SUBDEV1\\G-UG-3")]

        // CHILD domain user found in THIS domain group
        [TestCase("SUBDEV1\\user1", "IDMDEV1\\G-DL-1"), TestCase("SUBDEV1\\user1", "IDMDEV1\\G-UG-1")]
        [TestCase("SUBDEV1\\user2", "IDMDEV1\\G-DL-2"), TestCase("SUBDEV1\\user2", "IDMDEV1\\G-UG-2")]
        [TestCase("SUBDEV1\\user3", "IDMDEV1\\G-DL-3"), TestCase("SUBDEV1\\user3", "IDMDEV1\\G-UG-3")]
        public void IsSidInToken(string targetToFind, string targetTokensToCheck)
        {
            ISecurityPrincipal userPrincipal = this.directory.GetPrincipal(targetToFind);
            ISecurityPrincipal userOrGroupPrincipal = this.directory.GetPrincipal(targetTokensToCheck);

            Assert.IsTrue(this.directory.IsSidInPrincipalToken(userOrGroupPrincipal.Sid, userPrincipal, userOrGroupPrincipal.Sid.AccountDomainSid));
        }

        [TestCase("SUBDEV1\\user1", "IDMDEV1\\G-GG-1"), TestCase("SUBDEV1\\user2", "IDMDEV1\\G-GG-2"), TestCase("SUBDEV1\\user3", "IDMDEV1\\G-GG-3")]
        [TestCase("SUBDEV1\\G-DL-1", "IDMDEV1\\G-DL-1"), TestCase("SUBDEV1\\G-UG-1", "IDMDEV1\\G-UG-1"), TestCase("SUBDEV1\\G-GG-1", "IDMDEV1\\G-GG-1")]
        [TestCase("IDMDEV1\\G-DL-1", "IDMDEV1\\user1")]
        [TestCase("IDMDEV1\\user1", "SUBDEV1\\user1")]
        public void IsSidNotInToken(string targetToFind, string targetTokensToCheck)
        {
            ISecurityPrincipal userPrincipal = this.directory.GetPrincipal(targetToFind);
            ISecurityPrincipal userOrGroupPrincipal = this.directory.GetPrincipal(targetTokensToCheck);

            Assert.IsFalse(this.directory.IsSidInPrincipalToken(userOrGroupPrincipal.Sid, userPrincipal, userOrGroupPrincipal.Sid.AccountDomainSid));
        }

        [TestCase("IDMDEV1\\PC1", "DC=IDMDEV1,DC=LOCAL")]
        [TestCase("IDMDEV1\\PC1", "OU=LAPS Testing,DC=IDMDEV1,DC=LOCAL")]
        [TestCase("IDMDEV1\\PC1", "OU=Computers,OU=LAPS Testing,DC=IDMDEV1,DC=LOCAL")]
        [TestCase("SUBDEV1\\PC1", "OU=LAPS Testing,DC=SUBDEV1,DC=IDMDEV1,DC=LOCAL")]
        [TestCase("SUBDEV1\\PC1", "OU=Computers,OU=LAPS Testing,DC=SUBDEV1,DC=IDMDEV1,DC=LOCAL")]
        public void CheckComputerIsInOU(string computerName, string ou)
        {
            Assert.IsTrue(this.IsComputerInOU(computerName, ou));
        }

        [TestCase("IDMDEV1\\PC1", "OU=Domain Controllers, DC=IDMDEV1,DC=LOCAL")]
        [TestCase("IDMDEV1\\PC1", "OU=LAPS Testing,DC=SUBDEV1,DC=IDMDEV1,DC=LOCAL")]
        [TestCase("IDMDEV1\\PC1", "OU=Computers,OU=LAPS Testing, DC=SUBDEV1,DC=IDMDEV1,DC=LOCAL")]
        [TestCase("SUBDEV1\\PC1", "OU=LAPS Testing, DC=IDMDEV1,DC=LOCAL")]
        [TestCase("SUBDEV1\\PC1", "OU=Computers, OU=LAPS Testing,DC=IDMDEV1,DC=LOCAL")]
        public void CheckComputerIsNotInOU(string computerName, string ou)
        {
            Assert.IsFalse(this.IsComputerInOU(computerName, ou));
        }

        [TestCase("IDMDEV1\\PC1", "IDMDEV1\\PC1 Password")]
        [TestCase("SUBDEV1\\PC1", "SUBDEV1\\PC1 Password")]
        public void ValidateLapsPassword(string computerName, string expectedPassword)
        {
            PasswordData data = this.GetLapsPassword(computerName);
            Assert.AreEqual(expectedPassword, data.Value);
        }

        [TestCase("IDMDEV1\\PC1", 9999999999999)]
        [TestCase("SUBDEV1\\PC1", 9999999999999)]
        public void ValidateLapsPasswordExpiry(string computerName, long expiryDate)
        {
            PasswordData data = this.GetLapsPassword(computerName);
            Assert.AreEqual(expiryDate, data?.ExpirationTime?.ToFileTimeUtc());
        }

        [TestCase("IDMDEV1\\PC1$", "PC1$", "CN=PC1,OU=Computers,OU=LAPS Testing,DC=IDMDEV1,DC=LOCAL", "IDMDEV1")]
        [TestCase("IDMDEV1\\PC2", "PC2$", "CN=PC2,OU=Computers,OU=LAPS Testing,DC=IDMDEV1,DC=LOCAL", "IDMDEV1")]
        [TestCase("SUBDEV1\\PC1$", "PC1$", "CN=PC1,OU=Computers,OU=LAPS Testing,DC=SUBDEV1,DC=IDMDEV1,DC=LOCAL", "SUBDEV1")]
        [TestCase("SUBDEV1\\PC2", "PC2$", "CN=PC2,OU=Computers,OU=LAPS Testing,DC=SUBDEV1,DC=IDMDEV1,DC=LOCAL", "SUBDEV1")]
        public void ValidateComputerDetails(string computerToGet, string samAccountName, string dn, string domain)
        {
            var computer = this.directory.GetComputer(computerToGet);

            Assert.AreEqual(samAccountName, computer.SamAccountName);
            var d = (NTAccount)computer.Sid.Translate(typeof(NTAccount));

            Assert.AreEqual(domain, d.Value.Split('\\')[0]);

            string qualifiedName = computerToGet.EndsWith("$") ? computerToGet : computerToGet + "$";

            Assert.AreEqual(qualifiedName, d.Value);
            Assert.AreEqual(dn, computer.DistinguishedName);
        }

        [TestCase("IDMDEV1\\user1", "user1", "CN=user1,OU=LAPS Testing,DC=IDMDEV1,DC=LOCAL", "IDMDEV1")]
        [TestCase("SUBDEV1\\user1", "user1", "CN=user1,OU=LAPS Testing,DC=SUBDEV1,DC=IDMDEV1,DC=LOCAL", "SUBDEV1")]
        public void ValidateUserDetails(string userToGet, string samAccountName, string dn, string domain)
        {
            var user = this.directory.GetUser(userToGet);

            Assert.AreEqual(samAccountName, user.SamAccountName);
            var d = (NTAccount)user.Sid.Translate(typeof(NTAccount));

            Assert.AreEqual(domain, d.Value.Split('\\')[0]);
            Assert.AreEqual(userToGet, d.Value);
            Assert.AreEqual(dn, user.DistinguishedName);
        }

        [TestCase("DC=IDMDEV1,DC=LOCAL")]
        [TestCase("CN=Computers,DC=IDMDEV1,DC=LOCAL")]
        [TestCase("OU=Domain Controllers,DC=IDMDEV1,DC=LOCAL")]
        [TestCase("DC=SUBDEV1,DC=IDMDEV1,DC=LOCAL")]
        [TestCase("CN=Computers,DC=SUBDEV1,DC=IDMDEV1,DC=LOCAL")]
        [TestCase("OU=Domain Controllers,DC=SUBDEV1,DC=IDMDEV1,DC=LOCAL")]
        public void ValidateIsContainer(string dn)
        {
            Assert.IsTrue(this.directory.IsContainer(dn));
        }

        [TestCase("CN=G-GG-1,OU=LAPS Testing,DC=IDMDEV1,DC=LOCAL")]
        [TestCase("CN=user1,OU=LAPS Testing,DC=IDMDEV1,DC=LOCAL")]
        [TestCase("CN=PC1,OU=Computers,OU=LAPS Testing,DC=IDMDEV1,DC=LOCAL")]
        [TestCase("CN=G-GG-1,OU=LAPS Testing,DC=SUBDEV1,DC=IDMDEV1,DC=LOCAL")]
        [TestCase("CN=user1,OU=LAPS Testing,DC=SUBDEV1,DC=IDMDEV1,DC=LOCAL")]
        [TestCase("CN=PC1,OU=Computers,OU=LAPS Testing,DC=SUBDEV1,DC=IDMDEV1,DC=LOCAL")]
        public void ValidateIsNotContainer(string dn)
        {
            Assert.IsFalse(this.directory.IsContainer(dn));
        }

        [TestCase("IDMDEV1\\G-GG-1", "G-GG-1", "CN=G-GG-1,OU=LAPS Testing,DC=IDMDEV1,DC=LOCAL", "IDMDEV1")]
        [TestCase("IDMDEV1\\G-DL-1", "G-DL-1", "CN=G-DL-1,OU=LAPS Testing,DC=IDMDEV1,DC=LOCAL", "IDMDEV1")]
        [TestCase("SUBDEV1\\G-GG-1", "G-GG-1", "CN=G-GG-1,OU=LAPS Testing,DC=SUBDEV1,DC=IDMDEV1,DC=LOCAL", "SUBDEV1")]
        public void ValidateGroupDetails(string groupToGet, string samAccountName, string dn, string domain)
        {
            var group = this.directory.GetGroup(groupToGet);

            Assert.AreEqual(samAccountName, group.SamAccountName);
            var d = (NTAccount)group.Sid.Translate(typeof(NTAccount));
            Assert.AreEqual(domain, d.Value.Split('\\')[0]);
            Assert.AreEqual(groupToGet, d.Value);
            Assert.AreEqual(dn, group.DistinguishedName);
        }

        [TestCase("PC1$")]
        [TestCase("PC2")]
        public void TestAmbiguousComputerNameLookup(string computerToGet)
        {
            Assert.Throws<AmbiguousNameException>(() => this.directory.GetComputer(computerToGet));
        }

        [TestCase("G-GG-1")]
        [TestCase("G-UG-1")]
        public void TestAmbiguousGroupNameLookup(string group)
        {
            Assert.Throws<AmbiguousNameException>(() => this.directory.GetGroup(group));
        }

        [TestCase("G-DG-1")]
        public void TestUnqualifiedDomainLocalGroupNotFound(string group)
        {
            Assert.Throws<ObjectNotFoundException>(() => this.directory.GetGroup(group));
        }

        [TestCase("user1")]
        [TestCase("user2")]
        [TestCase("user3")]
        public void TestAmbiguousUserNameLookup(string user)
        {
            Assert.Throws<AmbiguousNameException>(() => this.directory.GetUser(user));
        }

        [TestCase("IDMDEV1\\PC2")]
        [TestCase("SUBDEV1\\PC2")]
        public void TestSetPasswordExpiry(string computerName)
        {
            var computer = this.directory.GetComputer(computerName);
            DateTime now = DateTime.Now;

            this.directory.SetPasswordExpiryTime(computer, now);

            var password = this.directory.GetPassword(computer);

            Assert.AreEqual(now, password.ExpirationTime);
        }

        private PasswordData GetLapsPassword(string computerName)
        {
            var computer = this.directory.GetComputer(computerName);
            return this.directory.GetPassword(computer);
        }

        private bool IsComputerInOU(string computerName, string ou)
        {
            var computer = this.directory.GetComputer(computerName);
            return this.directory.IsComputerInOu(computer, ou);
        }
    }
}
