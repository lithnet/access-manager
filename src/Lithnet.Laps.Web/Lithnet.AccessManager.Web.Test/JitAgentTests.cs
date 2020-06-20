using System;
using System.Linq;
using Lithnet.AccessManager.Agent;
using Moq;
using Microsoft.Extensions.Logging;
using System.Security.Principal;
using System.Collections.Generic;
using NUnit.Framework;

namespace Lithnet.AccessManager.Test
{
    public class JitAgentTests
    {
        private IDirectory directory;

        private IAppDataProvider provider;

        private ILocalSam sam;

        [SetUp()]
        public void TestInitialize()
        {
            directory = new ActiveDirectory(Mock.Of<ILogger<ActiveDirectory>>());
            provider = new MsDsAppConfigurationProvider();
            sam = new LocalSam(Mock.Of<ILogger<LocalSam>>());
        }

        [TestCase("IDMDEV1\\G-DL-1", "IDMDEV1\\user1", "IDMDEV1\\user2")]
        [TestCase("IDMDEV1\\G-DL-1")]
        public void TestExpectedMembership(string expectedMemberName, params string[] otherMembers)
        {
            var settings = new Mock<IJitSettings>();
            settings.SetupGet(a => a.AllowedAdmins).Returns(otherMembers);

            JitAgent agent = new JitAgent(Mock.Of<ILogger<JitAgent>>(), directory, settings.Object, sam, provider);

            ISecurityPrincipal expectedMember1 = directory.GetPrincipal(expectedMemberName);

            var result = agent.BuildExpectedMembership(expectedMember1.Sid);

            List<SecurityIdentifier> expected = new List<SecurityIdentifier>();
            expected.Add(sam.GetWellKnownSid(WellKnownSidType.AccountAdministratorSid));
            expected.Add(expectedMember1.Sid);

            foreach (string otherMember in otherMembers)
            {
                expected.Add(directory.GetPrincipal(otherMember).Sid);
            }
            
            CollectionAssert.AreEquivalent(expected, result);
        }
    }
}