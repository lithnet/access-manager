using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Text;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Lithnet.AccessManager.Test
{

    /// <summary>
    /// These tests require the following objects to be on the local machine
    /// User: 'testlocal1' 
    /// Group: 'test-group'
    /// </summary>
    public class LocalSamTests
    {
        private const string testGroupName = "test-group";

        private Mock<NLog.ILogger> dummyLogger;

        private ILocalSam sam;

        private GroupPrincipal testGroup;

        private IDirectory directory;

        [SetUp()]
        public void TestInitialize()
        {
            dummyLogger = new Mock<NLog.ILogger>();
            sam = new LocalSam(Mock.Of<ILogger<LocalSam>>());
            directory = new ActiveDirectory(Mock.Of<ILogger<ActiveDirectory>>());
        }

        [Test]
        public void AddGroupMember()
        {
            this.SetupGroupTests();
            SecurityIdentifier admin = sam.GetWellKnownSid(WellKnownSidType.AccountAdministratorSid);

            sam.AddLocalGroupMember(testGroupName, admin);

            foreach (var p in testGroup.GetMembers())
            {
                Assert.AreEqual(admin, p.Sid);
            }

            //sam.UpdateLocalGroupMembership;
        }

        [Test]
        public void RemoveLocalGroupMember()
        {
            this.SetupGroupTests();
            SecurityIdentifier admin = sam.GetWellKnownSid(WellKnownSidType.AccountAdministratorSid);

            sam.AddLocalGroupMember(testGroupName, admin);

            foreach (var p in testGroup.GetMembers())
            {
                Assert.AreEqual(admin, p.Sid);
            }

            sam.RemoveLocalGroupMember(testGroupName, admin);
            Assert.AreEqual(0, testGroup.GetMembers().Count());
        }

        [Test]
        public void UpdateLocalGroupMembersRemoveOthers()
        {
            this.SetupGroupTests();
            SecurityIdentifier admin = sam.GetWellKnownSid(WellKnownSidType.AccountAdministratorSid);

            sam.AddLocalGroupMember(testGroupName, admin);

            foreach (var p in testGroup.GetMembers())
            {
                Assert.AreEqual(admin, p.Sid);
            }

            SecurityIdentifier user1 = this.directory.GetUser("IDMDEV1\\user1").Sid;
            SecurityIdentifier user2 = this.directory.GetUser("IDMDEV1\\user2").Sid;
            SecurityIdentifier user3 = this.directory.GetUser("IDMDEV1\\user3").Sid;
            var expected = new[] { user1, user2, user3 };

            sam.UpdateLocalGroupMembership(testGroupName, expected, false, false);

            CollectionAssert.AreEquivalent(expected, testGroup.GetMembers().Select(t => t.Sid));
        }

        [Test]
        public void UpdateLocalGroupMembersKeepOthers()
        {
            this.SetupGroupTests();
            SecurityIdentifier admin = sam.GetWellKnownSid(WellKnownSidType.AccountAdministratorSid);

            sam.AddLocalGroupMember(testGroupName, admin);

            foreach (var p in testGroup.GetMembers())
            {
                Assert.AreEqual(admin, p.Sid);
            }

            SecurityIdentifier user1 = this.directory.GetUser("IDMDEV1\\user1").Sid;
            SecurityIdentifier user2 = this.directory.GetUser("IDMDEV1\\user2").Sid;
            SecurityIdentifier user3 = this.directory.GetUser("IDMDEV1\\user3").Sid;
            var expected = new List<SecurityIdentifier> { user1, user2, user3 };

            sam.UpdateLocalGroupMembership(testGroupName, expected, true, false);

            expected.Add(admin);

            CollectionAssert.AreEquivalent(expected, testGroup.GetMembers().Select(t => t.Sid));
        }

        [Test]
        public void GetLocalGroupMembers()
        {
            var result = sam.GetLocalGroupMembers("Administrators");
            var expected = this.GetLocalGroup("Administrators").GetMembers().Select(t => t.Sid);
            CollectionAssert.AreEquivalent(expected, result);
        }

        [Test]
        public void GetBuiltInAdministratorsGroupName()
        {
            Assert.AreEqual("Administrators", sam.GetBuiltInAdministratorsGroupName());
        }

        [Test]
        public void IsDomainController()
        {
            Assert.IsFalse(sam.IsDomainController());
        }

        [Test]
        public void GetMachineNetbiosDomainName()
        {
            Assert.AreEqual(Environment.UserDomainName, sam.GetMachineNetbiosDomainName());
        }

        [Test]
        public void GetMachineNTAccountName()
        {
            Assert.AreEqual($"{Environment.UserDomainName}\\{Environment.MachineName}", sam.GetMachineNTAccountName());
        }

        [Test]
        public void SetPassword()
        {
            string newPassword = Guid.NewGuid().ToString();

            using (PrincipalContext c = new PrincipalContext(ContextType.Machine))
            {
                var user = UserPrincipal.FindByIdentity(c, "testlocal1");
                this.sam.SetLocalAccountPassword(user.Sid, newPassword);

                Assert.AreEqual(DateTime.UtcNow.Trim(TimeSpan.TicksPerMinute), user.LastPasswordSet.Value.Trim(TimeSpan.TicksPerMinute));
            }
        }

        private void SetupGroupTests()
        {
            this.DeleteLocalGroup(testGroupName);
            this.CreateLocalGroup(testGroupName);
            testGroup = this.GetLocalGroup(testGroupName);
        }

        private void CreateLocalGroup(string name)
        {
            using (PrincipalContext context = new PrincipalContext(ContextType.Machine))
            {
                GroupPrincipal g = new GroupPrincipal(context, name);
                g.Save();
            }
        }

        private void DeleteLocalGroup(string name)
        {
            using (PrincipalContext context = new PrincipalContext(ContextType.Machine))
            {
                using (GroupPrincipal g = GroupPrincipal.FindByIdentity(context, name))
                {
                    g?.Delete();
                }
            }
        }

        private GroupPrincipal GetLocalGroup(string name)
        {
            PrincipalContext context = new PrincipalContext(ContextType.Machine);
            return GroupPrincipal.FindByIdentity(context, name);
        }
    }
}
