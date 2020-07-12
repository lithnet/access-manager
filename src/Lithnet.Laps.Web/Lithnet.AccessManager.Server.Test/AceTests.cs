using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using Lithnet.AccessManager.Server;
using Lithnet.Security.Authorization;
using Moq;
using NUnit.Framework;

namespace Lithnet.AccessManager.Web.Test
{
    public class AceTests
    {
        private Mock<NLog.ILogger> dummyLogger;

        [SetUp()]
        public void TestInitialize()
        {
            dummyLogger = new Mock<NLog.ILogger>();
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
            ActiveDirectory d = new  ActiveDirectory();
            Assert.IsTrue(this.IsMatch(acePrincipal, requestor, null));
        }

        // These cases fail because the AuthzInitializeContextFromSid API fails when used with a one-way trust
        //[TestCase("extdev1\\G-DL-1", "idmdev1\\user1")]
        //[TestCase("extdev1\\G-DL-1", "subdev1\\user1")]
        //[TestCase("extdev1\\G-DL-2", "idmdev1\\user2")]
        //[TestCase("extdev1\\G-DL-2", "subdev1\\user2")]
        //[TestCase("extdev1\\G-DL-3", "idmdev1\\user3")]
        //[TestCase("extdev1\\G-DL-3", "subdev1\\user3")]

        // Test to make sure mismatched group membership is not a match
        [TestCase("subdev1\\G-DL-2", "idmdev1\\user1", "subdev1.idmdev1.local")]
        [TestCase("subdev1\\G-DL-2", "subdev1\\user1", "subdev1.idmdev1.local")]
        [TestCase("subdev1\\G-DL-3", "idmdev1\\user2", "subdev1.idmdev1.local")]
        [TestCase("subdev1\\G-DL-3", "subdev1\\user2", "subdev1.idmdev1.local")]
        [TestCase("subdev1\\G-DL-1", "idmdev1\\user3", "subdev1.idmdev1.local")]
        [TestCase("subdev1\\G-DL-1", "subdev1\\user3", "subdev1.idmdev1.local")]

        [TestCase("idmdev1\\G-DL-2", "idmdev1\\user1", "idmdev1.local")]
        [TestCase("idmdev1\\G-DL-2", "subdev1\\user1", "idmdev1.local")]
        [TestCase("idmdev1\\G-DL-3", "idmdev1\\user2", "idmdev1.local")]
        [TestCase("idmdev1\\G-DL-3", "subdev1\\user2", "idmdev1.local")]
        [TestCase("idmdev1\\G-DL-1", "idmdev1\\user3", "idmdev1.local")]
        [TestCase("idmdev1\\G-DL-1", "subdev1\\user3", "idmdev1.local")]
        public void TestAceNotMatch(string trustee, string requestor, string servername)
        {
            Assert.IsFalse(this.IsMatch(trustee, requestor, servername));
        }

        [TestCase("idmdev1\\G-XX-1", "idmdev1\\user3")] // Test a group we know doesn't exist in THIS domain
        [TestCase("subdev1\\G-XX-1", "subdev1\\user3")] // Test a group we know doesn't exist in CHILD domain
        [TestCase("extdev1\\G-XX-1", "extdev1\\user3")] // Test a group we know doesn't exist in EXT domain
        public void TestAceExceptionThrownOnDeny(string trustee, string requestor)
        {
            Assert.Throws<ObjectNotFoundException>(() => this.IsMatch(trustee, requestor, null, AccessControlType.Deny));
        }

        private bool IsMatch(string trustee, string requestor, string serverName, AccessControlType aceType = AccessControlType.Allow)
        {
            ActiveDirectory d = new ActiveDirectory();
            var user = d.GetUser(requestor);
            var p = d.GetPrincipal(trustee);

            DiscretionaryAcl dacl = new DiscretionaryAcl(false, false, 1);
            dacl.AddAccess(aceType, p.Sid, (int)AccessMask.Jit, InheritanceFlags.None, PropagationFlags.None);
            CommonSecurityDescriptor sd = new CommonSecurityDescriptor(false, false, ControlFlags.DiscretionaryAclPresent, new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null), null, null, dacl);

            if (serverName == null)
            {
                serverName = d.GetDnsDomainName(p.Sid);
            }

            AuthorizationContext c = new AuthorizationContext(user.Sid, serverName);
            
            return c.AccessCheck(sd, (int) Server.AccessMask.Jit);
        }
    }
}