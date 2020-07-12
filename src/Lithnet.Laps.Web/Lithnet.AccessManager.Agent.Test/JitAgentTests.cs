using System;
using Moq;
using Microsoft.Extensions.Logging;
using System.Security.Principal;
using System.Collections.Generic;
using NUnit.Framework;

namespace Lithnet.AccessManager.Agent.Test
{
    public class JitAgentTests
    {
        private Mock<IAppDataProvider> mockProvider;

        private Mock<ILocalSam> mockSam;

        private Mock<IJitSettings> mockSettings;

        private JitAgent agent;

        private ActiveDirectory directory;

        private LocalSam sam;

        [SetUp()]
        public void TestInitialize()
        {
            this.directory = new ActiveDirectory();
            this.sam = new LocalSam(Mock.Of<ILogger<LocalSam>>());

            this.mockProvider = new Mock<IAppDataProvider>();
            this.mockSam = new Mock<ILocalSam>();
            this.mockSettings = new Mock<IJitSettings>();

            this.agent = new JitAgent(Mock.Of<ILogger<JitAgent>>(), this.directory, this.mockSettings.Object, this.mockSam.Object);
        }

        [TestCase("IDMDEV1\\G-DL-1", "IDMDEV1\\user1", "IDMDEV1\\user2")]
        [TestCase("IDMDEV1\\G-DL-1")]
        public void TestExpectedMembership(string expectedMemberName, params string[] otherMembers)
        {
            mockSettings.SetupGet(a => a.AllowedAdmins).Returns(otherMembers);
            mockSam.Setup(a => a.GetWellKnownSid(WellKnownSidType.AccountAdministratorSid)).Returns(this.sam.GetWellKnownSid(WellKnownSidType.AccountAdministratorSid));

            ISecurityPrincipal expectedMember1 = directory.GetPrincipal(expectedMemberName);

            var result = agent.BuildExpectedMembership(expectedMember1.Sid);

            List<SecurityIdentifier> expected = new List<SecurityIdentifier>
            {
                this.sam.GetWellKnownSid(WellKnownSidType.AccountAdministratorSid),
                expectedMember1.Sid
            };

            foreach (string otherMember in otherMembers)
            {
                expected.Add(this.directory.GetPrincipal(otherMember).Sid);
            }

            CollectionAssert.AreEquivalent(expected, result);
        }


        [Test]
        public void TestExitOnAgentDisabled()
        {
            this.mockSettings.SetupGet(a => a.JitEnabled).Returns(false);

            agent.DoCheck();
            mockSettings.VerifyGet(t => t.JitEnabled);
        }

        [Test]
        public void TestGetGroupFromName()
        {
            string groupName = "IDMDEV1\\G-DL-1";

            mockSettings.SetupGet(a => a.JitGroup).Returns(groupName);
            IGroup group = agent.GetJitGroup();
            Assert.AreEqual(groupName, group.MsDsPrincipalName.ToUpper());
        }

        [Test]
        public void TestGetGroupFromSid()
        {
            string groupName = "IDMDEV1\\G-DL-1";
            IGroup g = this.directory.GetGroup(groupName);
            mockSettings.SetupGet(a => a.JitGroup).Returns(g.Sid.ToString());
            IGroup group = agent.GetJitGroup();
            Assert.AreEqual(g.Sid, group.Sid);
        }


        [Test]
        public void TestGetGroupNameWithNameTemplate()
        {
            mockSettings.SetupGet(a => a.JitGroup).Returns("IDMDEV1\\JIT-{computerName}");
            Assert.IsTrue(agent.TryGetGroupName(out string groupName));
            Assert.AreEqual($"IDMDEV1\\JIT-{Environment.MachineName}", groupName);
        }

        [Test]
        public void TestGetGroupNameWithFixedName()
        {
            mockSettings.SetupGet(a => a.JitGroup).Returns("IDMDEV1\\JIT-PC1");
            Assert.IsTrue(agent.TryGetGroupName(out string groupName));
            Assert.AreEqual($"IDMDEV1\\JIT-PC1", groupName);
        }

        [Test]
        public void TestGetGroupNameWithNoDomain()
        {
            mockSettings.SetupGet(a => a.JitGroup).Returns("JIT-PC1");
            mockSam.Setup(a => a.GetMachineNetbiosDomainName()).Returns(Environment.UserDomainName);
            Assert.IsTrue(agent.TryGetGroupName(out string groupName));
            Assert.AreEqual($"IDMDEV1\\JIT-PC1", groupName);
        }

        [Test]
        public void TestGetGroupNameWithSid()
        {
            string sid = WindowsIdentity.GetCurrent().User.ToString();
            mockSettings.SetupGet(a => a.JitGroup).Returns(sid);
            Assert.IsTrue(agent.TryGetGroupName(out string groupName));
            Assert.AreEqual(sid, groupName);
        }

        [Test]
        public void TestGetGroupNameWithDomainTemplate()
        {
            mockSettings.SetupGet(a => a.JitGroup).Returns("{domain}\\JIT-{computerName}");
            mockSam.Setup(a => a.GetMachineNetbiosDomainName()).Returns(Environment.UserDomainName);

            Assert.IsTrue(agent.TryGetGroupName(out string groupName));
            Assert.AreEqual($"{Environment.UserDomainName}\\JIT-{Environment.MachineName}", groupName);
        }
    }
}