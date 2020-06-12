using Lithnet.Laps.Web.Authorization;
using Moq;
using NLog;
using NUnit.Framework;
using System;
using System.ComponentModel;

namespace Lithnet.Laps.Web.Test
{
    public class AceTests
    {
        private Mock<ILogger> dummyLogger;

        [SetUp()]
        public void TestInitialize()
        {
            dummyLogger = new Mock<ILogger>();
        }

        /// <summary>
        /// Test setup
        ///
        /// These tests ensure we are correctly able to resolve the membership of cross-domain groups.
        /// Membership of domain local groups can only be resolved in the domain the group is located in.
        ///
        /// For this test, we have 3 domains.
        ///  - The domain in which the unit test is run which in this case is 'idmdev1'
        ///  - A child domain of idmdev1 called 'subdev1'
        ///  - An external forest that trusts idmdev1 called 'extdev1'
        ///
        /// 3 users are created in each domain, user1, user2, and user3
        /// 9 groups are created in each domain. 3 domain local groups (G-DL-x), 3 global groups (G-GG-x), and 3 universal groups (G-UG-x). Where x is a number of 1-3
        /// Each user is added to a domain local, global, and universal group, with a number matching their user name. So user1 is added to G-DL-1, G-GG-1, and G-UG-1
        /// To each G-DL-1 group, add the G-UG-1 groups from the other domains
        /// To each G-DL-2 group, directly add the 'user2' objects from the other domains
        /// To each G-DL-3 group, add the G-GG-3 groups from the other domains
        /// To each G-UG-1 group, add the G-GG-1 groups from the other domains
        /// To each G-UG-2 group, add the G-UG-2 groups from the other domains
        /// To each G-UG-3 group, add the 'user3' objects from the other domains
        ///
        /// This will ensure that each support group has 3 users in it, one from each domain, and allow us to query for the existence of those users when processing the ACE in the various domains
        ///
        /// Note that as domain local groups cannot be used outside of the domain in which they are created, their membership can never be resolved outside of the domain they exist in. As such, the tests that check for the DL group memberships against another domain are expected to not match the ACE entry, and are included on a separate test below.
        ///
        /// With a one-way trust, group membership for the users from the trusted domain cannot be resolved in the trusting domain. This means any group membership in the trusting domain is currently not visible.
        ///
        /// </summary>
        /// <param name="acePrincipal">The name of the user or group that grants access</param>
        /// <param name="computer">The 'target' computer that we are simulating checking an ACE on. The computer itself doesn't play a part in its test, rather we use the computers domain to determine where to build the user's token. This way domain local groups in the domain where the computer exists will be respected</param>
        /// <param name="requestor">The user who is requesting access to the resource that we are checking the ACE against</param>

        // Test global groups in THIS domain against a user in THIS domain
        [TestCase("idmdev1\\G-GG-1", "idmdev1\\user1")]
        [TestCase("idmdev1\\G-GG-2", "idmdev1\\user2")]
        [TestCase("idmdev1\\G-GG-3", "idmdev1\\user3")]

        // Test global groups in CHILD domain against a user in CHILD domain
        [TestCase("subdev1\\G-GG-1", "subdev1\\user1")]
        [TestCase("subdev1\\G-GG-2", "subdev1\\user2")]
        [TestCase("subdev1\\G-GG-3", "subdev1\\user3")]

        // Test domain local groups in THIS domain against a user in THIS domain
        [TestCase("idmdev1\\G-DL-1", "idmdev1\\user1")]
        [TestCase("idmdev1\\G-DL-2", "idmdev1\\user2")]
        [TestCase("idmdev1\\G-DL-3", "idmdev1\\user3")]

        // Test domain local groups in THIS domain against a user in CHILD domain
        [TestCase("idmdev1\\G-DL-1", "subdev1\\user1")]
        [TestCase("idmdev1\\G-DL-2", "subdev1\\user2")]
        [TestCase("idmdev1\\G-DL-3", "subdev1\\user3")]

        // Test universal groups in THIS domain against a user in THIS domain
        [TestCase("idmdev1\\G-UG-1", "idmdev1\\user1")]
        [TestCase("idmdev1\\G-UG-2", "idmdev1\\user2")]
        [TestCase("idmdev1\\G-UG-3", "idmdev1\\user3")]

        // Test universal groups in THIS domain against a user in CHILD domain
        [TestCase("idmdev1\\G-UG-1", "subdev1\\user1")]
        [TestCase("idmdev1\\G-UG-2", "subdev1\\user2")]
        [TestCase("idmdev1\\G-UG-3", "subdev1\\user3")]

        // Test universal groups in CHILD domain against a user in THIS domain
        [TestCase("subdev1\\G-UG-1", "idmdev1\\user1")]
        [TestCase("subdev1\\G-UG-2", "idmdev1\\user2")]
        [TestCase("subdev1\\G-UG-3", "idmdev1\\user3")]

        // Test universal groups in CHILD domain against a user in CHILD domain
        [TestCase("subdev1\\G-UG-1", "subdev1\\user1")]
        [TestCase("subdev1\\G-UG-2", "subdev1\\user2")]
        [TestCase("subdev1\\G-UG-3", "subdev1\\user3")]

        // Test domain local groups in CHILD domain against a user in THIS domain
        [TestCase("subdev1\\G-DL-1", "idmdev1\\user1")]
        [TestCase("subdev1\\G-DL-2", "idmdev1\\user2")]
        [TestCase("subdev1\\G-DL-3", "idmdev1\\user3")]

        // Test domain local groups in CHILD domain against a user in CHILD domain
        [TestCase("subdev1\\G-DL-1", "subdev1\\user1")]
        [TestCase("subdev1\\G-DL-2", "subdev1\\user2")]
        [TestCase("subdev1\\G-DL-3", "subdev1\\user3")]

        // Test domain local groups in EXT EXT domain against a user in EXT domain
        [TestCase("extdev1\\G-DL-1", "extdev1\\user1")]
        [TestCase("extdev1\\G-DL-2", "extdev1\\user2")]
        [TestCase("extdev1\\G-DL-3", "extdev1\\user3")]

        // Test global groups in EXT EXT domain against a user in EXT domain
        [TestCase("extdev1\\G-GG-1", "extdev1\\user1")]
        [TestCase("extdev1\\G-GG-2", "extdev1\\user2")]
        [TestCase("extdev1\\G-GG-3", "extdev1\\user3")]

        // Test universal groups in EXT EXT domain against a user in EXT domain
        [TestCase("extdev1\\G-UG-1", "extdev1\\user1")]
        [TestCase("extdev1\\G-UG-2", "extdev1\\user2")]
        [TestCase("extdev1\\G-UG-3", "extdev1\\user3")]
        public void TestAceMatch(string acePrincipal, string requestor)
        {
            Assert.IsTrue(this.IsMatch(acePrincipal, requestor));
        }


        // These cases fail because the AuthzInitializeContextFromSid API fails when used with a one-way trust
        [TestCase("extdev1\\G-DL-1", "idmdev1\\user1")]
        [TestCase("extdev1\\G-DL-1", "subdev1\\user1")]
        [TestCase("extdev1\\G-DL-2", "idmdev1\\user2")]
        [TestCase("extdev1\\G-DL-2", "subdev1\\user2")]
        [TestCase("extdev1\\G-DL-3", "idmdev1\\user3")]
        [TestCase("extdev1\\G-DL-3", "subdev1\\user3")]

        // Test to make sure mismatched group membership is not a match
        [TestCase("subdev1\\G-DL-2", "idmdev1\\user1")]
        [TestCase("subdev1\\G-DL-2", "subdev1\\user1")]
        [TestCase("subdev1\\G-DL-3", "idmdev1\\user2")]
        [TestCase("subdev1\\G-DL-3", "subdev1\\user2")]
        [TestCase("subdev1\\G-DL-1", "idmdev1\\user3")]
        [TestCase("subdev1\\G-DL-1", "subdev1\\user3")]

        [TestCase("idmdev1\\G-DL-2", "idmdev1\\user1")]
        [TestCase("idmdev1\\G-DL-2", "subdev1\\user1")]
        [TestCase("idmdev1\\G-DL-3", "idmdev1\\user2")]
        [TestCase("idmdev1\\G-DL-3", "subdev1\\user2")]
        [TestCase("idmdev1\\G-DL-1", "idmdev1\\user3")]
        [TestCase("idmdev1\\G-DL-1", "subdev1\\user3")]
        public void TestAceNotMatch(string trustee, string requestor)
        {
            Assert.IsFalse(this.IsMatch(trustee, requestor));
        }

        [TestCase("idmdev1\\G-XX-1", "idmdev1\\user3")] // Test a group we know doesn't exist in THIS domain
        [TestCase("subdev1\\G-XX-1", "subdev1\\user3")] // Test a group we know doesn't exist in CHILD domain
        [TestCase("extdev1\\G-XX-1", "extdev1\\user3")] // Test a group we know doesn't exist in EXT domain
        public void TestAceExceptionThrownOnDeny(string trustee, string requestor)
        {
            Assert.Throws<ObjectNotFoundException>(() => this.IsMatch(trustee, requestor, AceType.Deny));
        }

        [TestCase("idmdev1\\G-XX-1", "idmdev1\\user3")] // Test a group we know doesn't exist in THIS domain
        [TestCase("subdev1\\G-XX-1", "subdev1\\user3")] // Test a group we know doesn't exist in CHILD domain
        [TestCase("extdev1\\G-XX-1", "extdev1\\user3")] // Test a group we know doesn't exist in EXT domain

        public void TestAceExceptionIgnoredOnAllow(string trustee, string requestor)
        {
            Assert.IsFalse(this.IsMatch(trustee, requestor, AceType.Allow));
        }

        private bool IsMatch(string trustee, string requestor, AceType aceType = AceType.Allow)
        {
            Mock<IAce> ace = new Mock<IAce>();
            ace.SetupGet(a => a.Name).Returns(trustee);
            ace.SetupGet(x => x.Type).Returns(aceType);

            ActiveDirectory.ActiveDirectory d = new ActiveDirectory.ActiveDirectory();

            AceEvaluator evaluator = new AceEvaluator(d, dummyLogger.Object);

            return evaluator.IsMatchingAce(ace.Object, d.GetUser(requestor));
        }
    }
}