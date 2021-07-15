using System.Security.AccessControl;
using System.Security.Principal;
using Lithnet.AccessManager.Server.Configuration;
using Lithnet.Security.Authorization;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using C = Lithnet.AccessManager.Test.TestEnvironmentConstants;

namespace Lithnet.AccessManager.Server.Test
{
    public class AceTests
    {
        private IActiveDirectory directory;

        private IDiscoveryServices discoveryServices;

        [SetUp()]
        public void TestInitialize()
        {
            this.discoveryServices = new DiscoveryServices(Mock.Of<ILogger<DiscoveryServices>>());
            this.directory = new ActiveDirectory(discoveryServices);
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
        [TestCase(C.DEV_G_GG_1, C.DEV_User1)]
        [TestCase(C.DEV_G_GG_2, C.DEV_User2)]
        [TestCase(C.DEV_G_GG_3, C.DEV_User3)]

        // Test global groups in CHILD domain against a user in CHILD domain
        [TestCase(C.SUBDEV_G_GG_1, C.SUBDEV_User1)]
        [TestCase(C.SUBDEV_G_GG_2, C.SUBDEV_User2)]
        [TestCase(C.SUBDEV_G_GG_3, C.SUBDEV_User3)]

        // Test domain local groups in THIS domain against a user in THIS domain
        [TestCase(C.DEV_G_DL_1, C.DEV_User1)]
        [TestCase(C.DEV_G_DL_2, C.DEV_User2)]
        [TestCase(C.DEV_G_DL_3, C.DEV_User3)]

        // Test domain local groups in THIS domain against a user in CHILD domain
        [TestCase(C.DEV_G_DL_1, C.SUBDEV_User1)]
        [TestCase(C.DEV_G_DL_2, C.SUBDEV_User2)]
        [TestCase(C.DEV_G_DL_3, C.SUBDEV_User3)]

        // Test universal groups in THIS domain against a user in THIS domain
        [TestCase(C.DEV_G_UG_1, C.DEV_User1)]
        [TestCase(C.DEV_G_UG_2, C.DEV_User2)]
        [TestCase(C.DEV_G_UG_3, C.DEV_User3)]

        // Test universal groups in THIS domain against a user in CHILD domain
        [TestCase(C.DEV_G_UG_1, C.SUBDEV_User1)]
        [TestCase(C.DEV_G_UG_2, C.SUBDEV_User2)]
        [TestCase(C.DEV_G_UG_3, C.SUBDEV_User3)]

        // Test universal groups in CHILD domain against a user in THIS domain
        [TestCase(C.SUBDEV_G_UG_1, C.DEV_User1)]
        [TestCase(C.SUBDEV_G_UG_2, C.DEV_User2)]
        [TestCase(C.SUBDEV_G_UG_3, C.DEV_User3)]

        // Test universal groups in CHILD domain against a user in CHILD domain
        [TestCase(C.SUBDEV_G_UG_1, C.SUBDEV_User1)]
        [TestCase(C.SUBDEV_G_UG_2, C.SUBDEV_User2)]
        [TestCase(C.SUBDEV_G_UG_3, C.SUBDEV_User3)]

        // Test domain local groups in CHILD domain against a user in THIS domain
        [TestCase(C.SUBDEV_G_DL_1, C.DEV_User1)]
        [TestCase(C.SUBDEV_G_DL_2, C.DEV_User2)]
        [TestCase(C.SUBDEV_G_DL_3, C.DEV_User3)]

        // Test domain local groups in CHILD domain against a user in CHILD domain
        [TestCase(C.SUBDEV_G_DL_1, C.SUBDEV_User1)]
        [TestCase(C.SUBDEV_G_DL_2, C.SUBDEV_User2)]
        [TestCase(C.SUBDEV_G_DL_3, C.SUBDEV_User3)]

        // Test domain local groups in EXT EXT domain against a user in EXT domain
        [TestCase(C.EXTDEV_G_DL_1, C.EXTDEV_User1)]
        [TestCase(C.EXTDEV_G_DL_2, C.EXTDEV_User2)]
        [TestCase(C.EXTDEV_G_DL_3, C.EXTDEV_User3)]

        // Test global groups in EXT EXT domain against a user in EXT domain
        [TestCase(C.EXTDEV_G_GG_1, C.EXTDEV_User1)]
        [TestCase(C.EXTDEV_G_GG_2, C.EXTDEV_User2)]
        [TestCase(C.EXTDEV_G_GG_3, C.EXTDEV_User3)]

        // Test universal groups in EXT EXT domain against a user in EXT domain
        [TestCase(C.EXTDEV_G_UG_1, C.EXTDEV_User1)]
        [TestCase(C.EXTDEV_G_UG_2, C.EXTDEV_User2)]
        [TestCase(C.EXTDEV_G_UG_3, C.EXTDEV_User3)]
        public void TestAceMatch(string acePrincipal, string requestor)
        {
            Assert.IsTrue(this.IsMatch(acePrincipal, requestor, null));
        }

        // These cases fail because the AuthzInitializeContextFromSid API fails when used with a one-way trust
        //[TestCase(C.EXTDEV_G_DL_1, C.DEV_User1)]
        //[TestCase(C.EXTDEV_G_DL_1, C.SUBDEV_User1)]
        //[TestCase(C.EXTDEV_G_DL_2, C.DEV_User2)]
        //[TestCase(C.EXTDEV_G_DL_2, C.SUBDEV_User2)]
        //[TestCase(C.EXTDEV_G_DL_3, C.DEV_User3)]
        //[TestCase(C.EXTDEV_G_DL_3, C.SUBDEV_User3)]

        // Test to make sure mismatched group membership is not a match
        [TestCase(C.SUBDEV_G_DL_2, C.DEV_User1, C.SubDevLocal)]
        [TestCase(C.SUBDEV_G_DL_2, C.SUBDEV_User1, C.SubDevLocal)]
        [TestCase(C.SUBDEV_G_DL_3, C.DEV_User2, C.SubDevLocal)]
        [TestCase(C.SUBDEV_G_DL_3, C.SUBDEV_User2, C.SubDevLocal)]
        [TestCase(C.SUBDEV_G_DL_1, C.DEV_User3, C.SubDevLocal)]
        [TestCase(C.SUBDEV_G_DL_1, C.SUBDEV_User3, C.SubDevLocal)]

        [TestCase(C.DEV_G_DL_2, C.DEV_User1, C.DevLocal)]
        [TestCase(C.DEV_G_DL_2, C.SUBDEV_User1, C.DevLocal)]
        [TestCase(C.DEV_G_DL_3, C.DEV_User2, C.DevLocal)]
        [TestCase(C.DEV_G_DL_3, C.SUBDEV_User2, C.DevLocal)]
        [TestCase(C.DEV_G_DL_1, C.DEV_User3, C.DevLocal)]
        [TestCase(C.DEV_G_DL_1, C.SUBDEV_User3, C.DevLocal)]
        public void TestAceNotMatch(string trustee, string requestor, string servername)
        {
            Assert.IsFalse(this.IsMatch(trustee, requestor, servername));
        }

        [TestCase(C.Dev + "\\G-XX-1", C.DEV_User3)] // Test a group we know doesn't exist in THIS domain
        [TestCase(C.SubDev + "\\G-XX-1", C.SUBDEV_User3)] // Test a group we know doesn't exist in CHILD domain
        [TestCase(C.ExtDev + "\\G-XX-1", C.EXTDEV_User3)] // Test a group we know doesn't exist in EXT domain
        public void TestAceExceptionThrownOnDeny(string trustee, string requestor)
        {
            Assert.Throws<ObjectNotFoundException>(() => this.IsMatch(trustee, requestor, null, AccessControlType.Deny));
        }

        private bool IsMatch(string trustee, string requestor, string domainName, AccessControlType aceType = AccessControlType.Allow)
        {
            var user = directory.GetUser(requestor);
            var p = directory.GetPrincipal(trustee);

            DiscretionaryAcl dacl = new DiscretionaryAcl(false, false, 1);
            dacl.AddAccess(aceType, p.Sid, (int)AccessMask.Jit, InheritanceFlags.None, PropagationFlags.None);
            CommonSecurityDescriptor sd = new CommonSecurityDescriptor(false, false, ControlFlags.DiscretionaryAclPresent, new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null), null, null, dacl);

            string serverName;

            if (domainName == null)
            {
                serverName = discoveryServices.GetDomainController(discoveryServices.GetDomainNameDns(p.Sid));
            }
            else
            {
                serverName = discoveryServices.GetDomainController(domainName);
            }

            using AuthorizationContext c = new AuthorizationContext(user.Sid, serverName);

            return c.AccessCheck(sd, (int)AccessMask.Jit);
        }
    }
}