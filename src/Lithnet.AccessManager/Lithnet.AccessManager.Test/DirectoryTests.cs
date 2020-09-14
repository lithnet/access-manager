using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using Lithnet.AccessManager.Interop;
using Lithnet.Security.Authorization;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using ILogger = NLog.ILogger;

namespace Lithnet.AccessManager.Test
{
    /// <summary>
    /// These test cases require two computers in each domain called 'PC1' and PC2' located in an OU called OU=Computers,OU=LAPS Testing at the root of the domain.
    /// This computers should have a LAPS password with the value "%computerDomain%\%ComputerName% Password" (eg "IDMDEV1\PC1 Password")
    /// The computers named PC1 should have an expiry date of 9999999999999
    ///
    /// These test cases also depend on the users and group structure defined in the AceTests class
    /// </summary>
    class DirectoryTests
    {
        private Mock<ILogger> dummyLogger;
        private ActiveDirectory directory;
        private IDiscoveryServices discoveryServices;

        [SetUp()]
        public void TestInitialize()
        {
            this.discoveryServices = new DiscoveryServices(Mock.Of<ILogger<DiscoveryServices>>());
            dummyLogger = new Mock<ILogger>();
            this.directory = new ActiveDirectory(this.discoveryServices);
        }

        [Test]
        public void TestAcl2()
        {
            var sid = WindowsIdentity.GetCurrent().User;
            SecurityIdentifier wks = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);

            DiscretionaryAcl dacl1 = new DiscretionaryAcl(false, false, 1);
            dacl1.AddAccess(AccessControlType.Allow, sid, 0x200, InheritanceFlags.None, PropagationFlags.None);
            CommonSecurityDescriptor csd = new CommonSecurityDescriptor(false, false, ControlFlags.DiscretionaryAclPresent, sid, sid, null, dacl1);

            DiscretionaryAcl dacl2 = new DiscretionaryAcl(false, false, 1);
            dacl2.AddAccess(AccessControlType.Allow, wks, 0x400, InheritanceFlags.None, PropagationFlags.None);
            CommonSecurityDescriptor csd2 = new CommonSecurityDescriptor(false, false, ControlFlags.DiscretionaryAclPresent, wks, wks, null, dacl2);

            List<GenericSecurityDescriptor> list = new List<GenericSecurityDescriptor>();
            list.Add(csd);
            list.Add(csd2);

            using AuthorizationContext c = new AuthorizationContext(WindowsIdentity.GetCurrent().AccessToken);
            Assert.IsTrue(c.AccessCheck(csd, 0x200));
            Assert.IsTrue(c.AccessCheck(list, 0x400));
            Assert.IsTrue(c.AccessCheck(list, 0x600));
            Assert.IsFalse(c.AccessCheck(csd, 0x800));
            Assert.IsFalse(c.AccessCheck(list, 0x800));
        }

        [Test]
        public void TestAcl3()
        {
            var sid = WindowsIdentity.GetCurrent().User;
            SecurityIdentifier wks = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);

            DiscretionaryAcl dacl1 = new DiscretionaryAcl(false, false, 1);
            dacl1.AddAccess(AccessControlType.Allow, sid, 0x200, InheritanceFlags.None, PropagationFlags.None);
            CommonSecurityDescriptor csd = new CommonSecurityDescriptor(false, false, ControlFlags.DiscretionaryAclPresent, sid, sid, null, dacl1);

            DiscretionaryAcl dacl2 = new DiscretionaryAcl(false, false, 1);
            dacl2.AddAccess(AccessControlType.Allow, wks, 0x400, InheritanceFlags.None, PropagationFlags.None);
            CommonSecurityDescriptor csd2 = new CommonSecurityDescriptor(false, false, ControlFlags.DiscretionaryAclPresent, wks, wks, null, dacl2);

            List<GenericSecurityDescriptor> list = new List<GenericSecurityDescriptor>();
            list.Add(csd);
            list.Add(csd2);

            using AuthorizationContext c = new AuthorizationContext(WindowsIdentity.GetCurrent().User);
            Assert.IsTrue(c.AccessCheck(csd, 0x200));
            Assert.IsTrue(c.AccessCheck(list, 0x400));
            Assert.IsTrue(c.AccessCheck(list, 0x600));
            Assert.IsFalse(c.AccessCheck(csd, 0x800));
            Assert.IsFalse(c.AccessCheck(list, 0x800));
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

        //[TestCase("IDMDEV1\\PC1", "IDMDEV1\\PC1 Password")]
        //[TestCase("SUBDEV1\\PC1", "SUBDEV1\\PC1 Password")]
        //public void ValidateLapsPassword(string computerName, string expectedPassword)
        //{
        //    PasswordData data = this.GetLapsPassword(computerName);
        //    Assert.AreEqual(expectedPassword, data.Value);
        //}

        //[TestCase("IDMDEV1\\PC1", 9999999999999)]
        //[TestCase("SUBDEV1\\PC1", 9999999999999)]
        //public void ValidateLapsPasswordExpiry(string computerName, long expiryDate)
        //{
        //    PasswordData data = this.GetLapsPassword(computerName);
        //    Assert.AreEqual(expiryDate, data?.ExpirationTime?.ToFileTimeUtc());
        //}

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
            DirectoryEntry de = new DirectoryEntry($"LDAP://{dn}");
            Assert.IsTrue(this.directory.IsContainer(de));
        }

        [TestCase("CN=G-GG-1,OU=LAPS Testing,DC=IDMDEV1,DC=LOCAL")]
        [TestCase("CN=user1,OU=LAPS Testing,DC=IDMDEV1,DC=LOCAL")]
        [TestCase("CN=PC1,OU=Computers,OU=LAPS Testing,DC=IDMDEV1,DC=LOCAL")]
        [TestCase("CN=G-GG-1,OU=LAPS Testing,DC=SUBDEV1,DC=IDMDEV1,DC=LOCAL")]
        [TestCase("CN=user1,OU=LAPS Testing,DC=SUBDEV1,DC=IDMDEV1,DC=LOCAL")]
        [TestCase("CN=PC1,OU=Computers,OU=LAPS Testing,DC=SUBDEV1,DC=IDMDEV1,DC=LOCAL")]
        public void ValidateIsNotContainer(string dn)
        {
            DirectoryEntry de = new DirectoryEntry($"LDAP://{dn}");
            Assert.IsFalse(this.directory.IsContainer(de));
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

        [Test]
        public void CreateTtlTestGroup()
        {
            string dc = discoveryServices.GetDomainController("idmdev1.local");

            this.directory.CreateTtlGroup("G-DL-Test-TTL", "G-DL-Test-TTL", "TTL test group", "OU=Computers,OU=Laps Testing,DC=idmdev1,DC=local", dc, TimeSpan.FromMinutes(1), GroupType.DomainLocal, true);
        }

        [Test]
        public void TestPamIsEnabled()
        {
            Assert.IsTrue(this.directory.IsPamFeatureEnabled(this.directory.GetUser("idmdev1\\user1").Sid, true));
        }

        [TestCase("IDMDEV1\\JIT-PC1", "IDMDEV1\\user1")]
        [TestCase("IDMDEV1\\JIT-PC1", "SUBDEV1\\user1")]
        [TestCase("EXTDEV1\\JIT-PC1", "EXTDEV1\\user1")]
        public void TestTimeBasedMembershipIntraForest(string groupName, string memberName)
        {
            IGroup group = this.directory.GetGroup(groupName);
            ISecurityPrincipal p = this.directory.GetUser(memberName);

            group.AddMember(p, TimeSpan.FromSeconds(10));

            Thread.Sleep(TimeSpan.FromSeconds(5));

            CollectionAssert.Contains(group.GetMemberDNs(), p.DistinguishedName);

            Thread.Sleep(TimeSpan.FromSeconds(15));

            CollectionAssert.DoesNotContain(group.GetMemberDNs(), p.DistinguishedName);
        }

        [TestCase("EXTDEV1\\JIT-PC1", "IDMDEV1\\user1")]
        [TestCase("EXTDEV1\\JIT-PC1", "SUBDEV1\\user1")]
        public void TestTimeBasedMembershipCrossForest(string groupName, string memberName)
        {
            IGroup group = this.directory.GetGroup(groupName);
            ISecurityPrincipal p = this.directory.GetUser(memberName);

            group.AddMember(p, TimeSpan.FromSeconds(10));

            Thread.Sleep(TimeSpan.FromSeconds(5));

            Assert.IsTrue(IsSidDnInGroup(group, p), "The user was not found in the group");

            Thread.Sleep(TimeSpan.FromSeconds(15));

            Assert.IsFalse(IsSidDnInGroup(group, p), "The user was still in the group");
        }

        private bool IsSidDnInGroup(IGroup group, ISecurityPrincipal p)
        {
            foreach (string dn in group.GetMemberDNs())
            {
                if (dn.StartsWith($"CN={p.Sid},", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsComputerInOU(string computerName, string ou)
        {
            var computer = this.directory.GetComputer(computerName);
            return this.directory.IsObjectInOu(computer, ou);
        }

        [Test]
        public void AddGroupMemberToTtlGroup()
        {
            string groupName = TestContext.CurrentContext.Random.GetString(10, "abcdefghijklmnop");
            string dc = discoveryServices.GetDomainController("idmdev1.local");
            this.directory.CreateTtlGroup(groupName, groupName, "TTL test group 2", "OU=Laps Testing,DC=idmdev1,DC=local", dc, TimeSpan.FromMinutes(1), GroupType.DomainLocal, true);

            Thread.Sleep(20000);
            IGroup group = this.directory.GetGroup($"IDMDEV1\\{groupName}");
            ISecurityPrincipal user = this.directory.GetUser("IDMDEV1\\user1");

            group.AddMember(user);

            CollectionAssert.Contains(group.GetMemberDNs(), user.DistinguishedName);

            this.directory.DeleteGroup($"IDMDEV1\\{groupName}");
        }

        public void TryGetGroup()
        {
            IGroup group = this.directory.GetGroup("IDMDEV1\\G-GG-1");
            Assert.IsTrue(this.directory.TryGetGroup(group.Sid, out IGroup group2));
            Assert.AreEqual(group.Sid, group2.Sid);
        }

        public void TryGetUser()
        {
            Assert.IsTrue(this.directory.TryGetUser("IDMDEV1\\user1", out _));
            Assert.IsFalse(this.directory.TryGetUser("IDMDEV1\\doesntexist", out _));
        }


        public void TryGetComputer()
        {
            Assert.IsTrue(this.directory.TryGetUser("IDMDEV1\\PC1", out _));
            Assert.IsFalse(this.directory.TryGetUser("IDMDEV1\\doesntexist", out _));
        }

        public void TryGetPrincipal()
        {
            Assert.IsTrue(this.directory.TryGetPrincipal("IDMDEV1\\PC1", out _)); ;
            Assert.IsFalse(this.directory.TryGetPrincipal("IDMDEV1\\doesntexist", out _));
        }
    }
}
