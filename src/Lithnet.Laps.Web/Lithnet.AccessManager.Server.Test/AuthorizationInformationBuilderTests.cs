using System;
using System.Collections.Generic;
using System.Security.AccessControl;
using System.Security.Principal;
using Lithnet.AccessManager.Server.Authorization;
using Lithnet.AccessManager.Server.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;

namespace Lithnet.AccessManager.Server.Test
{
    public class AuthorizationInformationBuilderTests
    {
        private IDirectory directory;

        private AuthorizationInformationMemoryCache cache;

        private AuthorizationInformationBuilder builder;

        private ILogger<AuthorizationInformationBuilder> logger;

        private IPowerShellSecurityDescriptorGenerator powershell;

        private Mock<IOptionsSnapshot<BuiltInProviderOptions>> optionsSnapshot;

        [SetUp()]
        public void TestInitialize()
        {
            directory = new ActiveDirectory();
            cache = new AuthorizationInformationMemoryCache();
            logger = Global.LogFactory.CreateLogger<AuthorizationInformationBuilder>();
            powershell = Mock.Of<IPowerShellSecurityDescriptorGenerator>();
            optionsSnapshot = new Mock<IOptionsSnapshot<BuiltInProviderOptions>>();
        }

        [TestCase("IDMDEV1\\user1", "IDMDEV1\\PC1", AccessMask.Laps, AccessMask.None, AccessMask.Laps)]
        [TestCase("IDMDEV1\\user1", "IDMDEV1\\PC1", AccessMask.Laps, AccessMask.Laps, AccessMask.None)]
        [TestCase("IDMDEV1\\user1", "IDMDEV1\\PC1", AccessMask.None, AccessMask.Laps, AccessMask.None)]
        [TestCase("IDMDEV1\\user1", "IDMDEV1\\PC1", AccessMask.LapsHistory, AccessMask.None, AccessMask.LapsHistory)]
        [TestCase("IDMDEV1\\user1", "IDMDEV1\\PC1", AccessMask.LapsHistory, AccessMask.LapsHistory, AccessMask.None)]
        [TestCase("IDMDEV1\\user1", "IDMDEV1\\PC1", AccessMask.None, AccessMask.LapsHistory, AccessMask.None)]
        [TestCase("IDMDEV1\\user1", "IDMDEV1\\PC1", AccessMask.Jit, AccessMask.None, AccessMask.Jit)]
        [TestCase("IDMDEV1\\user1", "IDMDEV1\\PC1", AccessMask.Jit, AccessMask.Jit, AccessMask.None)]
        [TestCase("IDMDEV1\\user1", "IDMDEV1\\PC1", AccessMask.None, AccessMask.Jit, AccessMask.None)]
        [TestCase("IDMDEV1\\user1", "IDMDEV1\\PC1", AccessMask.None, AccessMask.None, AccessMask.None)]
        [TestCase("IDMDEV1\\user1", "IDMDEV1\\PC1", AccessMask.Laps | AccessMask.LapsHistory | AccessMask.Jit, AccessMask.Jit, AccessMask.Laps | AccessMask.LapsHistory)]
        [TestCase("IDMDEV1\\user1", "IDMDEV1\\PC1", AccessMask.Laps | AccessMask.LapsHistory | AccessMask.Jit, AccessMask.Laps, AccessMask.Jit | AccessMask.LapsHistory)]
        [TestCase("IDMDEV1\\user1", "IDMDEV1\\PC1", AccessMask.Laps | AccessMask.LapsHistory | AccessMask.Jit, AccessMask.LapsHistory, AccessMask.Laps | AccessMask.Jit)]
        public void TestAclAuthorizationOnComputerTarget(string username, string computerName, AccessMask allowed, AccessMask denied, AccessMask expected)
        {
            IUser user = directory.GetUser(username);
            IComputer computer = directory.GetComputer(computerName);

            SetupOptionsForComputerTarget(allowed, denied, computer, user);

            builder = new AuthorizationInformationBuilder(optionsSnapshot.Object, directory, logger, powershell, cache);
            var result = builder.GetAuthorizationInformation(user, computer);

            Assert.AreEqual(result.EffectiveAccess, expected);
        }

        [TestCase("IDMDEV1\\user1", "IDMDEV1\\PC1", "OU=Computers,OU=LAPS Testing,DC=IDMDEV1,DC=LOCAL", AccessMask.Laps, AccessMask.None, AccessMask.Laps)]
        [TestCase("IDMDEV1\\user1", "IDMDEV1\\PC1", "OU=LAPS Testing,DC=IDMDEV1,DC=LOCAL", AccessMask.Laps, AccessMask.None, AccessMask.Laps)]
        [TestCase("IDMDEV1\\user1", "IDMDEV1\\PC1", "DC=IDMDEV1,DC=LOCAL", AccessMask.Laps, AccessMask.None, AccessMask.Laps)]
        public void TestAclAuthorizationOnOUTarget(string username, string computerName, string targetOU, AccessMask allowed, AccessMask denied, AccessMask expected)
        {
            IUser user = directory.GetUser(username);
            IComputer computer = directory.GetComputer(computerName);

            SetupOptionsForOUTarget(allowed, denied, targetOU, user);

            builder = new AuthorizationInformationBuilder(optionsSnapshot.Object, directory, logger, powershell, cache);
            var result = builder.GetAuthorizationInformation(user, computer);

            Assert.AreEqual(result.EffectiveAccess, expected);
        }

        [Test]
        public void TestMatchingTargetsAllow()
        {
            IUser user = directory.GetUser("IDMDEV1\\user1");
            IComputer computer1 = directory.GetComputer("IDMDEV1\\PC1");
            IComputer computer2 = directory.GetComputer("IDMDEV1\\PC2");
            IGroup group1 = directory.GetGroup("IDMDEV1\\G-DL-PC1");
            IGroup group2 = directory.GetGroup("IDMDEV1\\G-DL-PC2");

            var t1 = CreateTarget(AccessMask.Laps, AccessMask.None, "OU=Computers,OU=LAPS Testing,DC=IDMDEV1,DC=LOCAL", user);
            var t2 = CreateTarget(AccessMask.Laps, AccessMask.None, "OU=LAPS Testing,DC=IDMDEV1,DC=LOCAL", user);
            var t3 = CreateTarget(AccessMask.Laps, AccessMask.None, "DC=IDMDEV1,DC=LOCAL", user);
            var t4 = CreateTarget(AccessMask.Laps, AccessMask.None, "OU=JIT Groups,OU=LAPS Testing,DC=IDMDEV1,DC=LOCAL", user);
            var t5 = CreateTarget(AccessMask.Laps, AccessMask.None, computer1, user);
            var t6 = CreateTarget(AccessMask.Laps, AccessMask.None, computer2, user);
            var t7 = CreateTarget(AccessMask.Laps, AccessMask.None, group1, user);
            var t8 = CreateTarget(AccessMask.Laps, AccessMask.None, group2, user);

            SetupOptions(t1, t2, t3, t4, t5, t6, t7, t8);

            builder = new AuthorizationInformationBuilder(optionsSnapshot.Object, directory, logger, powershell, cache);

            CollectionAssert.AreEquivalent(builder.GetMatchingTargetsForComputer(computer1), new[] { t1, t2, t3, t5 ,t7 });

            var result = builder.GetAuthorizationInformation(user, computer1);
            CollectionAssert.AreEquivalent(result.SuccessfulLapsTargets, new[] { t1, t2, t3, t5, t7 });
        }

        [Test]
        public void TestMatchingTargetsDenyOnOU()
        {
            IUser user = directory.GetUser("IDMDEV1\\user1");
            IComputer computer1 = directory.GetComputer("IDMDEV1\\PC1");
            IComputer computer2 = directory.GetComputer("IDMDEV1\\PC2");
            IGroup group1 = directory.GetGroup("IDMDEV1\\G-DL-PC1");
            IGroup group2 = directory.GetGroup("IDMDEV1\\G-DL-PC2");

            var t1 = CreateTarget(AccessMask.Laps, AccessMask.None, "OU=Computers,OU=LAPS Testing,DC=IDMDEV1,DC=LOCAL", user);
            var t2 = CreateTarget(AccessMask.Laps, AccessMask.None, "OU=LAPS Testing,DC=IDMDEV1,DC=LOCAL", user);
            var t3 = CreateTarget(AccessMask.None, AccessMask.Laps, "DC=IDMDEV1,DC=LOCAL", user);
            var t4 = CreateTarget(AccessMask.Laps, AccessMask.None, "OU=JIT Groups,OU=LAPS Testing,DC=IDMDEV1,DC=LOCAL", user);
            var t5 = CreateTarget(AccessMask.Laps, AccessMask.None, computer1, user);
            var t6 = CreateTarget(AccessMask.Laps, AccessMask.None, computer2, user);
            var t7 = CreateTarget(AccessMask.Laps, AccessMask.None, group1, user);
            var t8 = CreateTarget(AccessMask.Laps, AccessMask.None, group2, user);

            SetupOptions(t1, t2, t3, t4, t5, t6, t7, t8);

            builder = new AuthorizationInformationBuilder(optionsSnapshot.Object, directory, logger, powershell, cache);

            CollectionAssert.AreEquivalent(builder.GetMatchingTargetsForComputer(computer1), new[] { t1, t2, t3, t5, t7 });

            var result = builder.GetAuthorizationInformation(user, computer1);
            CollectionAssert.AreEquivalent(result.SuccessfulLapsTargets, new[] { t1, t2, t5, t7 });
            Assert.AreEqual(AccessMask.None, result.EffectiveAccess);
        }

        private void SetupOptionsForOUTarget(AccessMask allowed, AccessMask denied, string ou, IUser user)
        {
            SetupOptions(CreateTarget(allowed, denied, ou, user));
        }

        private void SetupOptionsForComputerTarget(AccessMask allowed, AccessMask denied, IComputer computer, IUser user)
        {
            SetupOptions(CreateTarget(allowed, denied, computer, user));
        }

        private void SetupOptions(params SecurityDescriptorTarget[] targets)
        {
            BuiltInProviderOptions options = new BuiltInProviderOptions();
            options.Targets = new List<SecurityDescriptorTarget>(targets);
            optionsSnapshot.SetupGet(t => t.Value).Returns((BuiltInProviderOptions)options);
        }

        private SecurityDescriptorTarget CreateTarget(AccessMask allowed, AccessMask denied, string ou, IUser user)
        {
            return new SecurityDescriptorTarget
            {
                AuthorizationMode = AuthorizationMode.SecurityDescriptor,
                Id = Guid.NewGuid().ToString(),
                Target = ou,
                Type = TargetType.Container,
                SecurityDescriptor = this.CreateSecurityDescriptor(user, allowed, denied)
            };
        }

        private SecurityDescriptorTarget CreateTarget(AccessMask allowed, AccessMask denied, IComputer computer, IUser user)
        {
            return new SecurityDescriptorTarget
            {
                AuthorizationMode = AuthorizationMode.SecurityDescriptor,
                Id = Guid.NewGuid().ToString(),
                Target = computer.Sid.ToString(),
                Type = TargetType.Computer,
                SecurityDescriptor = this.CreateSecurityDescriptor(user, allowed, denied)
            };
        }

        private SecurityDescriptorTarget CreateTarget(AccessMask allowed, AccessMask denied, IGroup group, IUser user)
        {
            return new SecurityDescriptorTarget
            {
                AuthorizationMode = AuthorizationMode.SecurityDescriptor,
                Id = Guid.NewGuid().ToString(),
                Target = group.Sid.ToString(),
                Type = TargetType.Group,
                SecurityDescriptor = this.CreateSecurityDescriptor(user, allowed, denied)
            };
        }

        private string CreateSecurityDescriptor(ISecurityPrincipal principal, AccessMask allowed, AccessMask denied)
        {
            DiscretionaryAcl dacl = new DiscretionaryAcl(false, false, 2);

            if (allowed > 0)
            {
                dacl.AddAccess(AccessControlType.Allow, principal.Sid, (int)allowed, InheritanceFlags.None, PropagationFlags.None);
            }

            if (denied > 0)
            {
                dacl.AddAccess(AccessControlType.Deny, principal.Sid, (int)denied, InheritanceFlags.None, PropagationFlags.None);
            }

            var sd = new CommonSecurityDescriptor(false, false, ControlFlags.DiscretionaryAclPresent, new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null), null, null, dacl);

            return sd.GetSddlForm(AccessControlSections.All);
        }

    }
}