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

        private ITargetDataProvider targetDataProvider;

        private IAuthorizationContextProvider authorizationContextProvider;


        [SetUp()]
        public void TestInitialize()
        {
            directory = new ActiveDirectory();
            cache = new AuthorizationInformationMemoryCache();
            logger = Global.LogFactory.CreateLogger<AuthorizationInformationBuilder>();
            powershell = Mock.Of<IPowerShellSecurityDescriptorGenerator>();
            targetDataProvider = new TargetDataProvider(new TargetDataCache(), Global.LogFactory.CreateLogger<TargetDataProvider>());
            authorizationContextProvider = new AuthorizationContextProvider(Mock.Of<IOptions<AuthorizationOptions>>(), directory, Global.LogFactory.CreateLogger<AuthorizationContextProvider>());
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

            var options = SetupOptionsForComputerTarget(allowed, denied, computer, user);

            builder = new AuthorizationInformationBuilder(options, directory, logger, powershell, cache, targetDataProvider, authorizationContextProvider);
            var result = builder.GetAuthorizationInformation(user, computer);

            Assert.AreEqual(expected, result.EffectiveAccess);
        }

        [TestCase("IDMDEV1\\user1", "IDMDEV1\\PC1", "OU=Computers,OU=LAPS Testing,DC=IDMDEV1,DC=LOCAL", AccessMask.Laps, AccessMask.None, AccessMask.Laps)]
        [TestCase("IDMDEV1\\user1", "IDMDEV1\\PC1", "OU=LAPS Testing,DC=IDMDEV1,DC=LOCAL", AccessMask.Laps, AccessMask.None, AccessMask.Laps)]
        [TestCase("IDMDEV1\\user1", "IDMDEV1\\PC1", "DC=IDMDEV1,DC=LOCAL", AccessMask.Laps, AccessMask.None, AccessMask.Laps)]
        public void TestAclAuthorizationOnOUTarget(string username, string computerName, string targetOU, AccessMask allowed, AccessMask denied, AccessMask expected)
        {
            IUser user = directory.GetUser(username);
            IComputer computer = directory.GetComputer(computerName);

            var options = SetupOptionsForOUTarget(allowed, denied, targetOU, user);

            builder = new AuthorizationInformationBuilder(options, directory, logger, powershell, cache, targetDataProvider, authorizationContextProvider);
            var result = builder.GetAuthorizationInformation(user, computer);

            Assert.AreEqual(expected, result.EffectiveAccess);
        }


        [TestCase("IDMDEV1")]
        [TestCase("SUBDEV1")]
        [TestCase("EXTDEV1")]
        public void GetMatchingTargetsForComputer(string targetDomain)
        {
            ISecurityPrincipal trustee = directory.GetPrincipal($"{targetDomain}\\user1");

            IComputer computer1 = directory.GetComputer($"{targetDomain}\\PC1");
            IComputer computer2 = directory.GetComputer($"{targetDomain}\\PC2");
            IGroup group1 = directory.GetGroup($"{targetDomain}\\G-DL-PC1");
            IGroup group2 = directory.GetGroup($"{targetDomain}\\G-DL-PC2");

            var namingContext = directory.TranslateName(targetDomain + "\\", Interop.DsNameFormat.Nt4Name, Interop.DsNameFormat.DistinguishedName);

            var t1 = CreateTarget(AccessMask.Laps, AccessMask.None, $"OU=Computers,OU=LAPS Testing,{namingContext}", trustee);
            var t2 = CreateTarget(AccessMask.Laps, AccessMask.None, $"OU=LAPS Testing,{namingContext}", trustee);
            var t3 = CreateTarget(AccessMask.Laps, AccessMask.None, $"{namingContext}", trustee);
            var t4 = CreateTarget(AccessMask.Laps, AccessMask.None, $"OU=JIT Groups,OU=LAPS Testing,{namingContext}", trustee);
            var t5 = CreateTarget(AccessMask.Laps, AccessMask.None, computer1, trustee);
            var t6 = CreateTarget(AccessMask.Laps, AccessMask.None, computer2, trustee);
            var t7 = CreateTarget(AccessMask.Laps, AccessMask.None, group1, trustee);
            var t8 = CreateTarget(AccessMask.Laps, AccessMask.None, group2, trustee);

            var options = SetupOptions(t1, t2, t3, t4, t5, t6, t7, t8);

            builder = new AuthorizationInformationBuilder(options, directory, logger, powershell, cache, targetDataProvider, authorizationContextProvider);

            CollectionAssert.AreEquivalent(new[] { t1, t2, t3, t5, t7 }, builder.GetMatchingTargetsForComputer(computer1));
        }

        [Test]
        public void ValidateTargetSortOrder()
        {
            ISecurityPrincipal trustee = directory.GetPrincipal("IDMDEV1\\user1");
            IComputer computer1 = directory.GetComputer("IDMDEV1\\PC1");

            var t1 = CreateTarget(AccessMask.Laps, AccessMask.None, "OU=LAPS Testing,DC=IDMDEV1,DC=LOCAL", trustee);
            var t2 = CreateTarget(AccessMask.Laps, AccessMask.None, "DC=IDMDEV1,DC=LOCAL", trustee);
            var t3 = CreateTarget(AccessMask.Laps, AccessMask.None, "OU=Computers,OU=LAPS Testing,DC=IDMDEV1,DC=LOCAL", trustee);
            var t4 = CreateTarget(AccessMask.Laps, AccessMask.None, "OU=JIT Groups,OU=LAPS Testing,DC=IDMDEV1,DC=LOCAL", trustee);

            var options = SetupOptions(t1, t2, t3, t4);

            builder = new AuthorizationInformationBuilder(options, directory, logger, powershell, cache, targetDataProvider, authorizationContextProvider);

            CollectionAssert.AreEqual(new[] { t3, t1, t2 }, builder.GetMatchingTargetsForComputer(computer1));
        }

        [TestCase("IDMDEV1\\user1", "IDMDEV1\\user1", "IDMDEV1")]
        [TestCase("IDMDEV1\\G-UG-1", "IDMDEV1\\user1", "IDMDEV1")]
        [TestCase("IDMDEV1\\user1", "IDMDEV1\\user1", "SUBDEV1")]
        [TestCase("IDMDEV1\\G-UG-1", "IDMDEV1\\user1", "SUBDEV1")]
        [TestCase("SUBDEV1\\user1", "SUBDEV1\\user1", "SUBDEV1")]
        [TestCase("SUBDEV1\\G-UG-1", "SUBDEV1\\user1", "SUBDEV1")]
        [TestCase("EXTDEV1\\user1", "EXTDEV1\\user1", "EXTDEV1")]
        [TestCase("EXTDEV1\\G-UG-1", "EXTDEV1\\user1", "EXTDEV1")]
        public void AllowTrusteeOnComputerTarget(string trusteeName, string requestorName, string targetDomain)
        {
            ISecurityPrincipal trustee = directory.GetPrincipal(trusteeName);
            IUser requestor = directory.GetUser(requestorName);

            IComputer computer1 = directory.GetComputer($"{targetDomain}\\PC1");
            IComputer computer2 = directory.GetComputer($"{targetDomain}\\PC2");
            IGroup group1 = directory.GetGroup($"{targetDomain}\\G-DL-PC1");
            IGroup group2 = directory.GetGroup($"{targetDomain}\\G-DL-PC2");

            var namingContext = directory.TranslateName(targetDomain + "\\", Interop.DsNameFormat.Nt4Name, Interop.DsNameFormat.DistinguishedName);

            var t1 = CreateTarget(AccessMask.None, AccessMask.None, $"OU=Computers,OU=LAPS Testing,{namingContext}", trustee);
            var t2 = CreateTarget(AccessMask.None, AccessMask.None, $"OU=LAPS Testing,{namingContext}", trustee);
            var t3 = CreateTarget(AccessMask.None, AccessMask.None, $"{namingContext}", trustee);
            var t4 = CreateTarget(AccessMask.None, AccessMask.None, $"OU=JIT Groups,OU=LAPS Testing,{namingContext}", trustee);
            var t5 = CreateTarget(AccessMask.Laps, AccessMask.None, computer1, trustee);
            var t6 = CreateTarget(AccessMask.None, AccessMask.None, computer2, trustee);
            var t7 = CreateTarget(AccessMask.None, AccessMask.None, group1, trustee);
            var t8 = CreateTarget(AccessMask.None, AccessMask.None, group2, trustee);

            var options = SetupOptions(t1, t2, t3, t4, t5, t6, t7, t8);

            builder = new AuthorizationInformationBuilder(options, directory, logger, powershell, cache, targetDataProvider, authorizationContextProvider);
            var result = builder.GetAuthorizationInformation(requestor, computer1);

            Assert.AreEqual(AccessMask.Laps, result.EffectiveAccess);

            CollectionAssert.AreEquivalent(new[] { t5 }, result.SuccessfulLapsTargets);
        }

        [TestCase("IDMDEV1\\user1", "IDMDEV1\\user1", "IDMDEV1")]
        [TestCase("IDMDEV1\\G-UG-1", "IDMDEV1\\user1", "IDMDEV1")]
        [TestCase("IDMDEV1\\user1", "IDMDEV1\\user1", "SUBDEV1")]
        [TestCase("IDMDEV1\\G-UG-1", "IDMDEV1\\user1", "SUBDEV1")]
        [TestCase("SUBDEV1\\user1", "SUBDEV1\\user1", "SUBDEV1")]
        [TestCase("SUBDEV1\\G-UG-1", "SUBDEV1\\user1", "SUBDEV1")]
        [TestCase("EXTDEV1\\user1", "EXTDEV1\\user1", "EXTDEV1")]
        [TestCase("EXTDEV1\\G-UG-1", "EXTDEV1\\user1", "EXTDEV1")]
        public void AllowTrusteeOnGroupTarget(string trusteeName, string requestorName, string targetDomain)
        {
            ISecurityPrincipal trustee = directory.GetPrincipal(trusteeName);
            IUser requestor = directory.GetUser(requestorName);

            IComputer computer1 = directory.GetComputer($"{targetDomain}\\PC1");
            IComputer computer2 = directory.GetComputer($"{targetDomain}\\PC2");
            IGroup group1 = directory.GetGroup($"{targetDomain}\\G-DL-PC1");
            IGroup group2 = directory.GetGroup($"{targetDomain}\\G-DL-PC2");

            var namingContext = directory.TranslateName(targetDomain + "\\", Interop.DsNameFormat.Nt4Name, Interop.DsNameFormat.DistinguishedName);

            var t1 = CreateTarget(AccessMask.None, AccessMask.None, $"OU=Computers,OU=LAPS Testing,{namingContext}", trustee);
            var t2 = CreateTarget(AccessMask.None, AccessMask.None, $"OU=LAPS Testing,{namingContext}", trustee);
            var t3 = CreateTarget(AccessMask.None, AccessMask.None, $"{namingContext}", trustee);
            var t4 = CreateTarget(AccessMask.None, AccessMask.None, $"OU=JIT Groups,OU=LAPS Testing,{namingContext}", trustee);
            var t5 = CreateTarget(AccessMask.None, AccessMask.None, computer1, trustee);
            var t6 = CreateTarget(AccessMask.None, AccessMask.None, computer2, trustee);
            var t7 = CreateTarget(AccessMask.Laps, AccessMask.None, group1, trustee);
            var t8 = CreateTarget(AccessMask.None, AccessMask.None, group2, trustee);

            var options = SetupOptions(t1, t2, t3, t4, t5, t6, t7, t8);

            builder = new AuthorizationInformationBuilder(options, directory, logger, powershell, cache, targetDataProvider, authorizationContextProvider);
            var result = builder.GetAuthorizationInformation(requestor, computer1);

            Assert.AreEqual(AccessMask.Laps, result.EffectiveAccess);

            CollectionAssert.AreEquivalent(new[] { t7 }, result.SuccessfulLapsTargets);
        }

        [TestCase("IDMDEV1\\user1", "IDMDEV1\\user1", "IDMDEV1")]
        [TestCase("IDMDEV1\\G-UG-1", "IDMDEV1\\user1", "IDMDEV1")]
        [TestCase("IDMDEV1\\user1", "IDMDEV1\\user1", "SUBDEV1")]
        [TestCase("IDMDEV1\\G-UG-1", "IDMDEV1\\user1", "SUBDEV1")]
        [TestCase("SUBDEV1\\user1", "SUBDEV1\\user1", "SUBDEV1")]
        [TestCase("SUBDEV1\\G-UG-1", "SUBDEV1\\user1", "SUBDEV1")]
        [TestCase("EXTDEV1\\user1", "EXTDEV1\\user1", "EXTDEV1")]
        [TestCase("EXTDEV1\\G-UG-1", "EXTDEV1\\user1", "EXTDEV1")]
        public void AllowTrusteeOnOUTarget(string trusteeName, string requestorName, string targetDomain)
        {
            ISecurityPrincipal trustee = directory.GetPrincipal(trusteeName);
            IUser requestor = directory.GetUser(requestorName);

            IComputer computer1 = directory.GetComputer($"{targetDomain}\\PC1");
            IComputer computer2 = directory.GetComputer($"{targetDomain}\\PC2");
            IGroup group1 = directory.GetGroup($"{targetDomain}\\G-DL-PC1");
            IGroup group2 = directory.GetGroup($"{targetDomain}\\G-DL-PC2");

            var namingContext = directory.TranslateName(targetDomain + "\\", Interop.DsNameFormat.Nt4Name, Interop.DsNameFormat.DistinguishedName);

            var t1 = CreateTarget(AccessMask.None, AccessMask.None, $"OU=Computers,OU=LAPS Testing,{namingContext}", trustee);
            var t2 = CreateTarget(AccessMask.None, AccessMask.None, $"OU=LAPS Testing,{namingContext}", trustee);
            var t3 = CreateTarget(AccessMask.Laps, AccessMask.None, $"{namingContext}", trustee);
            var t4 = CreateTarget(AccessMask.None, AccessMask.None, $"OU=JIT Groups,OU=LAPS Testing,{namingContext}", trustee);
            var t5 = CreateTarget(AccessMask.None, AccessMask.None, computer1, trustee);
            var t6 = CreateTarget(AccessMask.None, AccessMask.None, computer2, trustee);
            var t7 = CreateTarget(AccessMask.None, AccessMask.None, group1, trustee);
            var t8 = CreateTarget(AccessMask.None, AccessMask.None, group2, trustee);

            var options = SetupOptions(t1, t2, t3, t4, t5, t6, t7, t8);

            builder = new AuthorizationInformationBuilder(options, directory, logger, powershell, cache, targetDataProvider, authorizationContextProvider);
            var result = builder.GetAuthorizationInformation(requestor, computer1);

            Assert.AreEqual(AccessMask.Laps, result.EffectiveAccess);

            CollectionAssert.AreEquivalent(new[] { t3 }, result.SuccessfulLapsTargets);
        }

        [TestCase("IDMDEV1\\user1", "IDMDEV1\\user1", "IDMDEV1")]
        [TestCase("IDMDEV1\\G-UG-1", "IDMDEV1\\user1", "IDMDEV1")]
        [TestCase("IDMDEV1\\user1", "IDMDEV1\\user1", "SUBDEV1")]
        [TestCase("IDMDEV1\\G-UG-1", "IDMDEV1\\user1", "SUBDEV1")]
        [TestCase("SUBDEV1\\user1", "SUBDEV1\\user1", "SUBDEV1")]
        [TestCase("SUBDEV1\\G-UG-1", "SUBDEV1\\user1", "SUBDEV1")]
        [TestCase("EXTDEV1\\user1", "EXTDEV1\\user1", "EXTDEV1")]
        [TestCase("EXTDEV1\\G-UG-1", "EXTDEV1\\user1", "EXTDEV1")]
        public void DenyTrusteeOnComputerTarget(string trusteeName, string requestorName, string targetDomain)
        {
            ISecurityPrincipal trustee = directory.GetPrincipal(trusteeName);
            IUser requestor = directory.GetUser(requestorName);

            IComputer computer1 = directory.GetComputer($"{targetDomain}\\PC1");
            IComputer computer2 = directory.GetComputer($"{targetDomain}\\PC2");
            IGroup group1 = directory.GetGroup($"{targetDomain}\\G-DL-PC1");
            IGroup group2 = directory.GetGroup($"{targetDomain}\\G-DL-PC2");

            var namingContext = directory.TranslateName(targetDomain + "\\", Interop.DsNameFormat.Nt4Name, Interop.DsNameFormat.DistinguishedName);

            var t1 = CreateTarget(AccessMask.Laps, AccessMask.None, $"OU=Computers,OU=LAPS Testing,{namingContext}", trustee);
            var t2 = CreateTarget(AccessMask.Laps, AccessMask.None, $"OU=LAPS Testing,{namingContext}", trustee);
            var t3 = CreateTarget(AccessMask.Laps, AccessMask.None, $"{namingContext}", trustee);
            var t4 = CreateTarget(AccessMask.Laps, AccessMask.None, $"OU=JIT Groups,OU=LAPS Testing,{namingContext}", trustee);
            var t5 = CreateTarget(AccessMask.None, AccessMask.Laps, computer1, trustee);
            var t6 = CreateTarget(AccessMask.Laps, AccessMask.None, computer2, trustee);
            var t7 = CreateTarget(AccessMask.Laps, AccessMask.None, group1, trustee);
            var t8 = CreateTarget(AccessMask.Laps, AccessMask.None, group2, trustee);

            var options = SetupOptions(t1, t2, t3, t4, t5, t6, t7, t8);

            builder = new AuthorizationInformationBuilder(options, directory, logger, powershell, cache, targetDataProvider, authorizationContextProvider);
            var result = builder.GetAuthorizationInformation(requestor, computer1);
            Assert.AreEqual(AccessMask.None, result.EffectiveAccess);
            CollectionAssert.AreEquivalent(new[] { t1, t2, t3, t7 }, result.SuccessfulLapsTargets);
        }

        [TestCase("IDMDEV1\\user1", "IDMDEV1\\user1", "IDMDEV1")]
        [TestCase("IDMDEV1\\G-UG-1", "IDMDEV1\\user1", "IDMDEV1")]
        [TestCase("IDMDEV1\\user1", "IDMDEV1\\user1", "SUBDEV1")]
        [TestCase("IDMDEV1\\G-UG-1", "IDMDEV1\\user1", "SUBDEV1")]
        [TestCase("SUBDEV1\\user1", "SUBDEV1\\user1", "SUBDEV1")]
        [TestCase("SUBDEV1\\G-UG-1", "SUBDEV1\\user1", "SUBDEV1")]
        [TestCase("EXTDEV1\\user1", "EXTDEV1\\user1", "EXTDEV1")]
        [TestCase("EXTDEV1\\G-UG-1", "EXTDEV1\\user1", "EXTDEV1")]
        public void DenyTrusteeOnOUTarget(string trusteeName, string requestorName, string targetDomain)
        {
            ISecurityPrincipal trustee = directory.GetPrincipal(trusteeName);
            IUser requestor = directory.GetUser(requestorName);

            IComputer computer1 = directory.GetComputer($"{targetDomain}\\PC1");
            IComputer computer2 = directory.GetComputer($"{targetDomain}\\PC2");
            IGroup group1 = directory.GetGroup($"{targetDomain}\\G-DL-PC1");
            IGroup group2 = directory.GetGroup($"{targetDomain}\\G-DL-PC2");

            var namingContext = directory.TranslateName(targetDomain + "\\", Interop.DsNameFormat.Nt4Name, Interop.DsNameFormat.DistinguishedName);

            var t1 = CreateTarget(AccessMask.Laps, AccessMask.None, $"OU=Computers,OU=LAPS Testing,{namingContext}", trustee);
            var t2 = CreateTarget(AccessMask.Laps, AccessMask.Laps, $"OU=LAPS Testing,{namingContext}", trustee);
            var t3 = CreateTarget(AccessMask.Laps, AccessMask.None, $"{namingContext}", trustee);
            var t4 = CreateTarget(AccessMask.Laps, AccessMask.None, $"OU=JIT Groups,OU=LAPS Testing,{namingContext}", trustee);
            var t5 = CreateTarget(AccessMask.Laps, AccessMask.None, computer1, trustee);
            var t6 = CreateTarget(AccessMask.Laps, AccessMask.None, computer2, trustee);
            var t7 = CreateTarget(AccessMask.Laps, AccessMask.None, group1, trustee);
            var t8 = CreateTarget(AccessMask.Laps, AccessMask.None, group2, trustee);

            var options = SetupOptions(t1, t2, t3, t4, t5, t6, t7, t8);

            builder = new AuthorizationInformationBuilder(options, directory, logger, powershell, cache, targetDataProvider, authorizationContextProvider);
            var result = builder.GetAuthorizationInformation(requestor, computer1);
            CollectionAssert.AreEquivalent(new[] { t1, t3, t5, t7 }, result.SuccessfulLapsTargets);
            Assert.AreEqual(AccessMask.None, result.EffectiveAccess);
        }

        [TestCase("IDMDEV1\\user1", "IDMDEV1\\user1", "IDMDEV1")]
        [TestCase("IDMDEV1\\G-UG-1", "IDMDEV1\\user1", "IDMDEV1")]
        [TestCase("IDMDEV1\\user1", "IDMDEV1\\user1", "SUBDEV1")]
        [TestCase("IDMDEV1\\G-UG-1", "IDMDEV1\\user1", "SUBDEV1")]
        [TestCase("SUBDEV1\\user1", "SUBDEV1\\user1", "SUBDEV1")]
        [TestCase("SUBDEV1\\G-UG-1", "SUBDEV1\\user1", "SUBDEV1")]
        [TestCase("EXTDEV1\\user1", "EXTDEV1\\user1", "EXTDEV1")]
        [TestCase("EXTDEV1\\G-UG-1", "EXTDEV1\\user1", "EXTDEV1")]
        public void DenyTrusteeOnGroupTarget(string trusteeName, string requestorName, string targetDomain)
        {
            ISecurityPrincipal trustee = directory.GetPrincipal(trusteeName);
            IUser requestor = directory.GetUser(requestorName);

            IComputer computer1 = directory.GetComputer($"{targetDomain}\\PC1");
            IComputer computer2 = directory.GetComputer($"{targetDomain}\\PC2");
            IGroup group1 = directory.GetGroup($"{targetDomain}\\G-DL-PC1");
            IGroup group2 = directory.GetGroup($"{targetDomain}\\G-DL-PC2");

            var namingContext = directory.TranslateName(targetDomain + "\\", Interop.DsNameFormat.Nt4Name, Interop.DsNameFormat.DistinguishedName);

            var t1 = CreateTarget(AccessMask.Laps, AccessMask.None, $"OU=Computers,OU=LAPS Testing,{namingContext}", trustee);
            var t2 = CreateTarget(AccessMask.Laps, AccessMask.None, $"OU=LAPS Testing,{namingContext}", trustee);
            var t3 = CreateTarget(AccessMask.Laps, AccessMask.None, $"{namingContext}", trustee);
            var t4 = CreateTarget(AccessMask.Laps, AccessMask.None, $"OU=JIT Groups,OU=LAPS Testing,{namingContext}", trustee);
            var t5 = CreateTarget(AccessMask.Laps, AccessMask.None, computer1, trustee);
            var t6 = CreateTarget(AccessMask.Laps, AccessMask.None, computer2, trustee);
            var t7 = CreateTarget(AccessMask.None, AccessMask.Laps, group1, trustee);
            var t8 = CreateTarget(AccessMask.Laps, AccessMask.None, group2, trustee);

            var options = SetupOptions(t1, t2, t3, t4, t5, t6, t7, t8);

            builder = new AuthorizationInformationBuilder(options, directory, logger, powershell, cache, targetDataProvider, authorizationContextProvider);
            var result = builder.GetAuthorizationInformation(requestor, computer1);
            CollectionAssert.AreEquivalent(new[] { t1, t2, t5, t3 }, result.SuccessfulLapsTargets);
            Assert.AreEqual(AccessMask.None, result.EffectiveAccess);
        }


        [TestCase("IDMDEV1\\user1", "IDMDEV1\\user1", "IDMDEV1\\PC1")]
        [TestCase("IDMDEV1\\user1", "IDMDEV1\\user1", "SUBDEV1\\PC1")]
        [TestCase("IDMDEV1\\user1", "IDMDEV1\\user1", "EXTDEV1\\PC1")]
        [TestCase("SUBDEV1\\user1", "SUBDEV1\\user1", "IDMDEV1\\PC1")]
        [TestCase("SUBDEV1\\user1", "SUBDEV1\\user1", "SUBDEV1\\PC1")]
        [TestCase("SUBDEV1\\user1", "SUBDEV1\\user1", "EXTDEV1\\PC1")]
        [TestCase("EXTDEV1\\user1", "EXTDEV1\\user1", "EXTDEV1\\PC1")]
        public void UserCanAccessComputer(string requestorName, string trusteeName, string computerName)
        {
            IUser requestor = directory.GetUser(requestorName);
            ISecurityPrincipal trustee = directory.GetPrincipal(trusteeName);
            IComputer computer = directory.GetComputer(computerName);

            var t1 = CreateTarget(AccessMask.Laps, AccessMask.None, computer, trustee);

            var options = SetupOptions(t1);

            builder = new AuthorizationInformationBuilder(options, directory, logger, powershell, cache, targetDataProvider, authorizationContextProvider);
            var result = builder.GetAuthorizationInformation(requestor, computer);
            CollectionAssert.AreEquivalent(new[] { t1 }, result.SuccessfulLapsTargets);
            Assert.AreEqual(AccessMask.Laps, result.EffectiveAccess);
        }

        // IDMDEV1\\user1 can access PCs in all domains via global groups in their home domain
        [TestCase("IDMDEV1\\user1", "IDMDEV1\\G-GG-1", "IDMDEV1\\PC1")]
        [TestCase("IDMDEV1\\user1", "IDMDEV1\\G-GG-1", "SUBDEV1\\PC1")]
        [TestCase("IDMDEV1\\user1", "IDMDEV1\\G-GG-1", "EXTDEV1\\PC1")]

        // IDMDEV1\\user1 can access PCs in all domains via universal groups in their own forest
        [TestCase("IDMDEV1\\user1", "IDMDEV1\\G-UG-1", "IDMDEV1\\PC1")]
        [TestCase("IDMDEV1\\user1", "IDMDEV1\\G-UG-1", "SUBDEV1\\PC1")]
        [TestCase("IDMDEV1\\user1", "IDMDEV1\\G-UG-1", "EXTDEV1\\PC1")]
        [TestCase("IDMDEV1\\user1", "SUBDEV1\\G-UG-1", "IDMDEV1\\PC1")]
        [TestCase("IDMDEV1\\user1", "SUBDEV1\\G-UG-1", "SUBDEV1\\PC1")]
        [TestCase("IDMDEV1\\user1", "SUBDEV1\\G-UG-1", "EXTDEV1\\PC1")]

        // IDMDEV1\\user1 can access PCs in their own forest via domain local groups in each domain
        [TestCase("IDMDEV1\\user1", "IDMDEV1\\G-DL-1", "IDMDEV1\\PC1")]
        [TestCase("IDMDEV1\\user1", "SUBDEV1\\G-DL-1", "SUBDEV1\\PC1")]


        // SUBDEV1\\user1 can access PCs in all domains via global groups in their home domain
        [TestCase("SUBDEV1\\user1", "SUBDEV1\\G-GG-1", "IDMDEV1\\PC1")]
        [TestCase("SUBDEV1\\user1", "SUBDEV1\\G-GG-1", "SUBDEV1\\PC1")]
        [TestCase("SUBDEV1\\user1", "SUBDEV1\\G-GG-1", "EXTDEV1\\PC1")]

        // SUBDEV1\\user1 can access PCs in all domains via universal groups in their own forest
        [TestCase("SUBDEV1\\user1", "IDMDEV1\\G-UG-1", "IDMDEV1\\PC1")]
        [TestCase("SUBDEV1\\user1", "IDMDEV1\\G-UG-1", "SUBDEV1\\PC1")]
        [TestCase("SUBDEV1\\user1", "IDMDEV1\\G-UG-1", "EXTDEV1\\PC1")]
        [TestCase("SUBDEV1\\user1", "SUBDEV1\\G-UG-1", "IDMDEV1\\PC1")]
        [TestCase("SUBDEV1\\user1", "SUBDEV1\\G-UG-1", "SUBDEV1\\PC1")]
        [TestCase("SUBDEV1\\user1", "SUBDEV1\\G-UG-1", "EXTDEV1\\PC1")]

        // SUBDEV1\\user1 can access PCs in their own forest via domain local groups in each domain
        [TestCase("SUBDEV1\\user1", "IDMDEV1\\G-DL-1", "IDMDEV1\\PC1")]
        [TestCase("SUBDEV1\\user1", "SUBDEV1\\G-DL-1", "SUBDEV1\\PC1")]

        // EXTDEV1\\user1 can access PCs via global groups only in their home domain
        [TestCase("EXTDEV1\\user1", "EXTDEV1\\G-GG-1", "EXTDEV1\\PC1")]
        
        // EXTDEV1\\user1 can access PCs via universal groups in their home domain forest
        [TestCase("EXTDEV1\\user1", "EXTDEV1\\G-UG-1", "EXTDEV1\\PC1")]
        
        // EXTDEV1\\user1 can access PCs in their own forest via domain local groups
        [TestCase("EXTDEV1\\user1", "EXTDEV1\\G-DL-1", "EXTDEV1\\PC1")]

        public void GroupCanAccessComputer(string requestorName, string trusteeName, string computerName)
        {
            IUser requestor = directory.GetUser(requestorName);
            ISecurityPrincipal trustee = directory.GetPrincipal(trusteeName);
            IComputer computer = directory.GetComputer(computerName);

            var t1 = CreateTarget(AccessMask.Laps, AccessMask.None, computer, trustee);

            var options = SetupOptions(t1);

            builder = new AuthorizationInformationBuilder(options, directory, logger, powershell, cache, targetDataProvider, authorizationContextProvider);
            var result = builder.GetAuthorizationInformation(requestor, computer);

            CollectionAssert.AreEquivalent(new[] { t1 }, result.SuccessfulLapsTargets);
            Assert.AreEqual(AccessMask.Laps, result.EffectiveAccess);
        }


        private IOptionsSnapshot<AuthorizationOptions> SetupOptionsForOUTarget(AccessMask allowed, AccessMask denied, string ou, ISecurityPrincipal trustee)
        {
            return SetupOptions(CreateTarget(allowed, denied, ou, trustee));
        }

        private IOptionsSnapshot<AuthorizationOptions> SetupOptionsForComputerTarget(AccessMask allowed, AccessMask denied, IComputer computer, ISecurityPrincipal trustee)
        {
            return SetupOptions(CreateTarget(allowed, denied, computer, trustee));
        }

        private IOptionsSnapshot<AuthorizationOptions> SetupOptions(params SecurityDescriptorTarget[] targets)
        {
            AuthorizationOptions options = new AuthorizationOptions();
            options.ComputerTargets = new List<SecurityDescriptorTarget>(targets);

            Mock<IOptionsSnapshot<AuthorizationOptions>> optionsSnapshot = new Mock<IOptionsSnapshot<AuthorizationOptions>>();
            optionsSnapshot.SetupGet(t => t.Value).Returns((AuthorizationOptions)options);
            return optionsSnapshot.Object;
        }

        private SecurityDescriptorTarget CreateTarget(AccessMask allowed, AccessMask denied, string ou, ISecurityPrincipal trustee)
        {
            return new SecurityDescriptorTarget
            {
                AuthorizationMode = AuthorizationMode.SecurityDescriptor,
                Id = Guid.NewGuid().ToString(),
                Target = ou,
                Type = TargetType.Container,
                SecurityDescriptor = this.CreateSecurityDescriptor(trustee, allowed, denied)
            };
        }

        private SecurityDescriptorTarget CreateTarget(AccessMask allowed, AccessMask denied, ISecurityPrincipal principal, ISecurityPrincipal trustee)
        {
            return new SecurityDescriptorTarget
            {
                AuthorizationMode = AuthorizationMode.SecurityDescriptor,
                Id = Guid.NewGuid().ToString(),
                Target = principal.Sid.ToString(),
                Type = principal switch
                {
                    IGroup _ => TargetType.Group,
                    IComputer _ => TargetType.Computer,
                    _ => throw new NotImplementedException(),
                },
                SecurityDescriptor = this.CreateSecurityDescriptor(trustee, allowed, denied)
            };
        }

        private string CreateSecurityDescriptor(ISecurityPrincipal trustee, AccessMask allowed, AccessMask denied)
        {
            DiscretionaryAcl dacl = new DiscretionaryAcl(false, false, 2);

            if (allowed > 0)
            {
                dacl.AddAccess(AccessControlType.Allow, trustee.Sid, (int)allowed, InheritanceFlags.None, PropagationFlags.None);
            }

            if (denied > 0)
            {
                dacl.AddAccess(AccessControlType.Deny, trustee.Sid, (int)denied, InheritanceFlags.None, PropagationFlags.None);
            }

            var sd = new CommonSecurityDescriptor(false, false, ControlFlags.DiscretionaryAclPresent, new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null), null, null, dacl);

            return sd.GetSddlForm(AccessControlSections.All);
        }

    }
}