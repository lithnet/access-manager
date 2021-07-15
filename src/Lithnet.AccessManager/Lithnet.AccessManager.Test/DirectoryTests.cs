using Lithnet.Security.Authorization;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using C = Lithnet.AccessManager.Test.TestEnvironmentConstants;
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
        [TestCase(C.DEV_User1, C.DEV_G_DL_1), TestCase(C.DEV_User1, C.DEV_G_GG_1), TestCase(C.DEV_User1, C.DEV_G_UG_1)]
        [TestCase(C.DEV_User2, C.DEV_G_UG_2), TestCase(C.DEV_User2, C.DEV_G_GG_2), TestCase(C.DEV_User2, C.DEV_G_UG_2)]
        [TestCase(C.DEV_User3, C.DEV_G_DL_3), TestCase(C.DEV_User3, C.DEV_G_GG_3), TestCase(C.DEV_User3, C.DEV_G_UG_3)]

        // THIS domain user found in CHILD domain group
        [TestCase(C.DEV_User1, C.SUBDEV_G_DL_1), TestCase(C.DEV_User1, C.SUBDEV_G_UG_1)]
        [TestCase(C.DEV_User2, C.SUBDEV_G_DL_2), TestCase(C.DEV_User2, C.SUBDEV_G_UG_2)]
        [TestCase(C.DEV_User3, C.SUBDEV_G_DL_3), TestCase(C.DEV_User3, C.SUBDEV_G_UG_3)]

        // User principals match themselves
        [TestCase(C.DEV_User1, C.DEV_User1), TestCase(C.SUBDEV_User1, C.SUBDEV_User1)]

        // CHILD domain user found in CHILD domain group
        [TestCase(C.SUBDEV_User1, C.SUBDEV_G_DL_1), TestCase(C.SUBDEV_User1, C.SUBDEV_G_GG_1), TestCase(C.SUBDEV_User1, C.SUBDEV_G_UG_1)]
        [TestCase(C.SUBDEV_User2, C.SUBDEV_G_DL_2), TestCase(C.SUBDEV_User2, C.SUBDEV_G_GG_2), TestCase(C.SUBDEV_User2, C.SUBDEV_G_UG_2)]
        [TestCase(C.SUBDEV_User3, C.SUBDEV_G_DL_3), TestCase(C.SUBDEV_User3, C.SUBDEV_G_GG_3), TestCase(C.SUBDEV_User3, C.SUBDEV_G_UG_3)]

        // CHILD domain user found in THIS domain group
        [TestCase(C.SUBDEV_User1, C.DEV_G_DL_1), TestCase(C.SUBDEV_User1, C.DEV_G_UG_1)]
        [TestCase(C.SUBDEV_User2, C.DEV_G_DL_2), TestCase(C.SUBDEV_User2, C.DEV_G_UG_2)]
        [TestCase(C.SUBDEV_User3, C.DEV_G_DL_3), TestCase(C.SUBDEV_User3, C.DEV_G_UG_3)]
        public void IsSidInToken(string targetToFind, string targetTokensToCheck)
        {
            IActiveDirectorySecurityPrincipal userPrincipal = this.directory.GetPrincipal(targetToFind);
            IActiveDirectorySecurityPrincipal userOrGroupPrincipal = this.directory.GetPrincipal(targetTokensToCheck);

            Assert.IsTrue(this.directory.IsSidInPrincipalToken(userOrGroupPrincipal.Sid, userPrincipal, userOrGroupPrincipal.Sid.AccountDomainSid));
        }

        [TestCase(C.SUBDEV_User1, C.DEV_G_GG_1), TestCase(C.SUBDEV_User2, C.DEV_G_GG_2), TestCase(C.SUBDEV_User3, C.DEV_G_GG_3)]
        [TestCase(C.SUBDEV_G_DL_1, C.DEV_G_DL_1), TestCase(C.SUBDEV_G_UG_1, C.DEV_G_UG_1), TestCase(C.SUBDEV_G_GG_1, C.DEV_G_GG_1)]
        [TestCase(C.DEV_G_DL_1, C.DEV_User1)]
        [TestCase(C.DEV_User1, C.SUBDEV_User1)]
        public void IsSidNotInToken(string targetToFind, string targetTokensToCheck)
        {
            IActiveDirectorySecurityPrincipal userPrincipal = this.directory.GetPrincipal(targetToFind);
            IActiveDirectorySecurityPrincipal userOrGroupPrincipal = this.directory.GetPrincipal(targetTokensToCheck);

            Assert.IsFalse(this.directory.IsSidInPrincipalToken(userOrGroupPrincipal.Sid, userPrincipal, userOrGroupPrincipal.Sid.AccountDomainSid));
        }

        [TestCase(C.DEV_PC1, C.DevDN)]
        [TestCase(C.DEV_PC1, C.AmsTesting_DevDN)]
        [TestCase(C.DEV_PC1, C.Computers_AmsTesting_DevDN)]
        [TestCase(C.SUBDEV_PC1, C.AmsTesting_SubDevDN)]
        [TestCase(C.SUBDEV_PC1, C.Computers_AmsTesting_SubDevDN)]
        public void CheckComputerIsInOU(string computerName, string ou)
        {
            Assert.IsTrue(this.IsComputerInOU(computerName, ou));
        }

        [TestCase(C.DEV_PC1, "OU=Domain Controllers," + C.DevDN)]
        [TestCase(C.DEV_PC1, C.AmsTesting_SubDevDN)]
        [TestCase(C.DEV_PC1, C.Computers_AmsTesting_SubDevDN)]
        [TestCase(C.SUBDEV_PC1, C.AmsTesting_DevDN)]
        [TestCase(C.SUBDEV_PC1, C.Computers_AmsTesting_DevDN)]
        public void CheckComputerIsNotInOU(string computerName, string ou)
        {
            Assert.IsFalse(this.IsComputerInOU(computerName, ou));
        }

        [TestCase(C.DEV_PC1_D, C.PC1_D, "CN=" + C.PC1 + "," + C.Computers_AmsTesting_DevDN, C.Dev)]
        [TestCase(C.DEV_PC2, C.PC2_D, "CN=" + C.PC2 + "," + C.Computers_AmsTesting_DevDN, C.Dev)]
        [TestCase(C.SUBDEV_PC1_D, C.PC1_D, "CN=" + C.PC1 + "," + C.Computers_AmsTesting_SubDevDN, C.SubDev)]
        [TestCase(C.SUBDEV_PC2, C.PC2_D, "CN=" + C.PC2 + "," + C.Computers_AmsTesting_SubDevDN, C.SubDev)]
        public void ValidateComputerDetails(string computerToGet, string samAccountName, string dn, string domain)
        {
            var computer = this.directory.GetComputer(computerToGet);

            StringAssert.AreEqualIgnoringCase(samAccountName, computer.SamAccountName);
            var d = (NTAccount)computer.Sid.Translate(typeof(NTAccount));

            StringAssert.AreEqualIgnoringCase(domain, d.Value.Split('\\')[0]);

            string qualifiedName = computerToGet.EndsWith("$") ? computerToGet : computerToGet + "$";

            StringAssert.AreEqualIgnoringCase(qualifiedName, d.Value);
            StringAssert.AreEqualIgnoringCase(dn, computer.DistinguishedName);
        }

        [TestCase(C.DEV_User1, C.User1, "CN=" + C.User1 + "," + C.Users_AmsTesting_DevDN, C.Dev)]
        [TestCase(C.SUBDEV_User1, C.User1, "CN=" + C.User1 + "," + C.Users_AmsTesting_SubDevDN, C.SubDev)]
        public void ValidateUserDetails(string userToGet, string samAccountName, string dn, string domain)
        {
            var user = this.directory.GetUser(userToGet);

            StringAssert.AreEqualIgnoringCase(samAccountName, user.SamAccountName);
            var d = (NTAccount)user.Sid.Translate(typeof(NTAccount));

            StringAssert.AreEqualIgnoringCase(domain, d.Value.Split('\\')[0]);
            StringAssert.AreEqualIgnoringCase(userToGet, d.Value);
            StringAssert.AreEqualIgnoringCase(dn, user.DistinguishedName);
        }

        [TestCase(C.DevDN)]
        [TestCase("CN=Computers," + C.DevDN)]
        [TestCase("OU=Domain Controllers," + C.DevDN)]
        [TestCase(C.SubDevDN)]
        [TestCase("CN=Computers," + C.SubDevDN)]
        [TestCase("OU=Domain Controllers," + C.SubDevDN)]
        public void ValidateIsContainer(string dn)
        {
            DirectoryEntry de = new DirectoryEntry($"LDAP://{dn}");
            Assert.IsTrue(this.directory.IsContainer(de));
        }

        [TestCase("CN=G-GG-1," + C.Groups_AmsTesting_DevDN)]
        [TestCase("CN=user1," + C.Users_AmsTesting_DevDN)]
        [TestCase("CN=PC1," + C.Computers_AmsTesting_DevDN)]
        [TestCase("CN=G-GG-1," + C.Groups_AmsTesting_SubDevDN)]
        [TestCase("CN=user1," + C.Users_AmsTesting_SubDevDN)]
        [TestCase("CN=PC1," + C.Computers_AmsTesting_DevDN)]
        public void ValidateIsNotContainer(string dn)
        {
            DirectoryEntry de = new DirectoryEntry($"LDAP://{dn}");
            Assert.IsFalse(this.directory.IsContainer(de));
        }

        [TestCase(C.DEV_G_GG_1, C.G_GG_1, "CN=G-GG-1," + C.Groups_AmsTesting_DevDN, C.Dev)]
        [TestCase(C.DEV_G_DL_1, C.G_DL_1, "CN=G-DL-1," + C.Groups_AmsTesting_DevDN, C.Dev)]
        [TestCase(C.SUBDEV_G_GG_1, C.G_GG_1, "CN=G-GG-1," + C.Groups_AmsTesting_SubDevDN, C.SubDev)]
        public void ValidateGroupDetails(string groupToGet, string samAccountName, string dn, string domain)
        {
            var group = this.directory.GetGroup(groupToGet);

            StringAssert.AreEqualIgnoringCase(samAccountName, group.SamAccountName);
            var d = (NTAccount)group.Sid.Translate(typeof(NTAccount));
            StringAssert.AreEqualIgnoringCase(domain, d.Value.Split('\\')[0]);
            StringAssert.AreEqualIgnoringCase(groupToGet, d.Value);
            StringAssert.AreEqualIgnoringCase(dn, group.DistinguishedName);
        }

        [TestCase(C.PC1_D)]
        [TestCase(C.PC2)]
        public void TestAmbiguousComputerNameLookup(string computerToGet)
        {
            Assert.Throws<AmbiguousNameException>(() => this.directory.GetComputer(computerToGet));
        }

        [TestCase(C.G_GG_1)]
        [TestCase(C.G_UG_1)]
        public void TestAmbiguousGroupNameLookup(string group)
        {
            Assert.Throws<AmbiguousNameException>(() => this.directory.GetGroup(group));
        }

        [TestCase("GroupThatDoesntExist")]
        public void TestUnqualifiedDomainLocalGroupNotFound(string group)
        {
            Assert.Throws<ObjectNotFoundException>(() => this.directory.GetGroup(group));
        }

        [TestCase(C.User1)]
        [TestCase(C.User2)]
        [TestCase(C.User3)]
        public void TestAmbiguousUserNameLookup(string user)
        {
            Assert.Throws<AmbiguousNameException>(() => this.directory.GetUser(user));
        }

        [Test]
        public void TestPamIsEnabled()
        {
            Assert.IsTrue(this.directory.IsPamFeatureEnabled(this.directory.GetUser(C.DEV_User1).Sid, true));
        }

        [TestCase(C.DEV_JIT_PC1, C.DEV_User1)]
        [TestCase(C.DEV_JIT_PC1, C.SUBDEV_User1)]
        [TestCase(C.EXTDEV_JIT_PC1, C.EXTDEV_User1)]
        public void TestTimeBasedMembershipIntraForest(string groupName, string memberName)
        {
            IActiveDirectoryGroup group = this.directory.GetGroup(groupName);
            IActiveDirectorySecurityPrincipal p = this.directory.GetUser(memberName);

            group.AddMember(p, TimeSpan.FromSeconds(10));

            Thread.Sleep(TimeSpan.FromSeconds(5));

            CollectionAssert.Contains(group.GetMemberDNs(), p.DistinguishedName);

            Thread.Sleep(TimeSpan.FromSeconds(15));

            CollectionAssert.DoesNotContain(group.GetMemberDNs(), p.DistinguishedName);
        }

        [TestCase(C.EXTDEV_JIT_PC1, C.DEV_User1)]
        [TestCase(C.EXTDEV_JIT_PC1, C.SUBDEV_User1)]
        public void TestTimeBasedMembershipCrossForest(string groupName, string memberName)
        {
            IActiveDirectoryGroup group = this.directory.GetGroup(groupName);
            IActiveDirectorySecurityPrincipal p = this.directory.GetUser(memberName);

            group.AddMember(p, TimeSpan.FromSeconds(10));

            Thread.Sleep(TimeSpan.FromSeconds(5));

            Assert.IsTrue(IsSidDnInGroup(group, p), "The user was not found in the group");

            Thread.Sleep(TimeSpan.FromSeconds(15));

            Assert.IsFalse(IsSidDnInGroup(group, p), "The user was still in the group");
        }

        private bool IsSidDnInGroup(IActiveDirectoryGroup group, IActiveDirectorySecurityPrincipal p)
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
            string dc = discoveryServices.GetDomainController(C.DevLocal);
            this.directory.CreateTtlGroup(groupName, groupName, "TTL test group 2", C.AmsTesting_DevDN, dc, TimeSpan.FromMinutes(1), GroupType.DomainLocal, true);

            Thread.Sleep(20000);
            IActiveDirectoryGroup group = this.directory.GetGroup($"{C.Dev}\\{groupName}");
            IActiveDirectorySecurityPrincipal user = this.directory.GetUser(C.DEV_User1);

            group.AddMember(user);

            CollectionAssert.Contains(group.GetMemberDNs(), user.DistinguishedName);

            this.directory.DeleteGroup($"{C.Dev}\\{groupName}");
        }

        public void TryGetGroup()
        {
            IActiveDirectoryGroup group = this.directory.GetGroup(C.DEV_G_GG_1);
            Assert.IsTrue(this.directory.TryGetGroup(group.Sid, out IActiveDirectoryGroup group2));
            Assert.AreEqual(group.Sid, group2.Sid);
        }

        public void TryGetUser()
        {
            Assert.IsTrue(this.directory.TryGetUser(C.DEV_User1, out _));
            Assert.IsFalse(this.directory.TryGetUser($"{C.Dev}\\doesntexist", out _));
        }


        public void TryGetComputer()
        {
            Assert.IsTrue(this.directory.TryGetUser(C.DEV_PC1, out _));
            Assert.IsFalse(this.directory.TryGetUser($"{C.Dev}\\doesntexist", out _));
        }

        public void TryGetPrincipal()
        {
            Assert.IsTrue(this.directory.TryGetPrincipal(C.DEV_PC1, out _)); ;
            Assert.IsFalse(this.directory.TryGetPrincipal($"{C.Dev}\\doesntexist", out _));
        }
    }
}
