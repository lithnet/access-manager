using System;
using System.Linq;
using Lithnet.AccessManager.Agent;
using Moq;
using Microsoft.Extensions.Logging;
using System.Security.Principal;
using System.Collections.Generic;
using NUnit.Framework;
using Microsoft.AspNetCore.Server.IIS.Core;

namespace Lithnet.AccessManager.Agent.Test
{
    public class JitAgentTests
    {
        private Mock<IDirectory> mockDirectory;

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

            this.mockDirectory = new Mock<IDirectory>();
            this.mockProvider = new Mock<IAppDataProvider>();
            this.mockSam = new Mock<ILocalSam>();
            this.mockSettings = new Mock<IJitSettings>();

            this.agent = new JitAgent(Mock.Of<ILogger<JitAgent>>(), this.mockDirectory.Object, this.mockSettings.Object, this.mockSam.Object, this.mockProvider.Object);
        }

        [TestCase("IDMDEV1\\G-DL-1", "IDMDEV1\\user1", "IDMDEV1\\user2")]
        [TestCase("IDMDEV1\\G-DL-1")]
        public void TestExpectedMembership(string expectedMemberName, params string[] otherMembers)
        {
            mockSettings.SetupGet(a => a.AllowedAdmins).Returns(otherMembers);
            mockSam.Setup(a => a.GetWellKnownSid(WellKnownSidType.AccountAdministratorSid)).Returns(this.sam.GetWellKnownSid(WellKnownSidType.AccountAdministratorSid));

            foreach (string otherMember in otherMembers)
            {
                mockDirectory.Setup(a => a.GetPrincipal(otherMember)).Returns(this.directory.GetPrincipal(otherMember));
            }

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
            mockDirectory.Verify(t => t.GetComputer(It.IsAny<string>()), Times.Never);
        }
    }
}