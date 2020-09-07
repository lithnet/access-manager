using System;
using Moq;
using Microsoft.Extensions.Logging;
using System.Security.Principal;
using System.Collections.Generic;
using System.DirectoryServices;
using NUnit.Framework;

namespace Lithnet.AccessManager.Agent.Test
{
    public class JitAgentTests
    {
        private Mock<ILocalSam> mockSam;

        private Mock<IJitSettings> mockSettings;

        private Mock<IJitAccessGroupResolver> groupResolver;

        private JitAgent agent;

        private ActiveDirectory directory;

        private IDiscoveryServices discoveryServices;

        private LocalSam sam;

        [SetUp()]
        public void TestInitialize()
        {
            this.discoveryServices = new DiscoveryServices(Mock.Of<ILogger<DiscoveryServices>>());
            this.directory = new ActiveDirectory(this.discoveryServices);
            this.sam = new LocalSam(Mock.Of<ILogger<LocalSam>>());
            this.mockSam = new Mock<ILocalSam>();
            this.mockSettings = new Mock<IJitSettings>();
            this.groupResolver = new Mock<IJitAccessGroupResolver>();

            this.agent = new JitAgent(Mock.Of<ILogger<JitAgent>>(), this.directory, this.mockSettings.Object, this.mockSam.Object, this.groupResolver.Object);
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
    }
}