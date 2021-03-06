﻿using System;
using System.Collections.Generic;
using System.Security.AccessControl;
using System.Security.Principal;
using Lithnet.AccessManager.Enterprise;
using Lithnet.AccessManager.Interop;
using Lithnet.AccessManager.Server.Authorization;
using Lithnet.AccessManager.Server.Configuration;
using Lithnet.Licensing.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using C = Lithnet.AccessManager.Test.TestEnvironmentConstants;

namespace Lithnet.AccessManager.Server.Test
{
    public class AuthorizationInformationBuilderTests
    {
        private IDirectory directory;
        private AuthorizationInformationMemoryCache cache;
        private AuthorizationInformationBuilder builder;
        private ILogger<AuthorizationInformationBuilder> logger;
        private IPowerShellSecurityDescriptorGenerator powershell;
        private IComputerTargetProvider targetDataProvider;
        private IAuthorizationContextProvider authorizationContextProvider;
        private IDiscoveryServices discoveryServices;
        private IAmsLicenseManager licenseManager;

        [SetUp()]
        public void TestInitialize()
        {
            this.discoveryServices = new DiscoveryServices(Mock.Of<ILogger<DiscoveryServices>>());
            directory = new ActiveDirectory(discoveryServices);
            cache = new AuthorizationInformationMemoryCache();
            logger = Global.LogFactory.CreateLogger<AuthorizationInformationBuilder>();
            powershell = Mock.Of<IPowerShellSecurityDescriptorGenerator>();
            var mockLicenseManager = new Mock<IAmsLicenseManager>();
            mockLicenseManager.Setup(l => l.IsEnterpriseEdition()).Returns(true);
            mockLicenseManager.Setup(l => l.IsFeatureCoveredByFullLicense(It.IsAny<LicensedFeatures>())).Returns(true);
            mockLicenseManager.Setup(l => l.IsFeatureEnabled(It.IsAny<LicensedFeatures>())).Returns(true);
            this.licenseManager = mockLicenseManager.Object;

            targetDataProvider = new ComputerTargetProvider(directory,new TargetDataProvider(new TargetDataCache(), Global.LogFactory.CreateLogger<TargetDataProvider>()), Global.LogFactory.CreateLogger<ComputerTargetProvider>());
            authorizationContextProvider = new AuthorizationContextProvider(Mock.Of<IOptions<AuthorizationOptions>>(), Global.LogFactory.CreateLogger<AuthorizationContextProvider>(), discoveryServices);
        }

        [TestCase(C.DEV_User1, C.DEV_PC1, AccessMask.LocalAdminPassword, AccessMask.None, AccessMask.LocalAdminPassword)]
        [TestCase(C.DEV_User1, C.DEV_PC1, AccessMask.LocalAdminPassword, AccessMask.LocalAdminPassword, AccessMask.None)]
        [TestCase(C.DEV_User1, C.DEV_PC1, AccessMask.None, AccessMask.LocalAdminPassword, AccessMask.None)]
        [TestCase(C.DEV_User1, C.DEV_PC1, AccessMask.LocalAdminPasswordHistory, AccessMask.None, AccessMask.LocalAdminPasswordHistory)]
        [TestCase(C.DEV_User1, C.DEV_PC1, AccessMask.LocalAdminPasswordHistory, AccessMask.LocalAdminPasswordHistory, AccessMask.None)]
        [TestCase(C.DEV_User1, C.DEV_PC1, AccessMask.None, AccessMask.LocalAdminPasswordHistory, AccessMask.None)]
        [TestCase(C.DEV_User1, C.DEV_PC1, AccessMask.Jit, AccessMask.None, AccessMask.Jit)]
        [TestCase(C.DEV_User1, C.DEV_PC1, AccessMask.Jit, AccessMask.Jit, AccessMask.None)]
        [TestCase(C.DEV_User1, C.DEV_PC1, AccessMask.None, AccessMask.Jit, AccessMask.None)]
        [TestCase(C.DEV_User1, C.DEV_PC1, AccessMask.None, AccessMask.None, AccessMask.None)]
        [TestCase(C.DEV_User1, C.DEV_PC1, AccessMask.LocalAdminPassword | AccessMask.LocalAdminPasswordHistory | AccessMask.Jit, AccessMask.Jit, AccessMask.LocalAdminPassword | AccessMask.LocalAdminPasswordHistory)]
        [TestCase(C.DEV_User1, C.DEV_PC1, AccessMask.LocalAdminPassword | AccessMask.LocalAdminPasswordHistory | AccessMask.Jit, AccessMask.LocalAdminPassword, AccessMask.Jit | AccessMask.LocalAdminPasswordHistory)]
        [TestCase(C.DEV_User1, C.DEV_PC1, AccessMask.LocalAdminPassword | AccessMask.LocalAdminPasswordHistory | AccessMask.Jit, AccessMask.LocalAdminPasswordHistory, AccessMask.LocalAdminPassword | AccessMask.Jit)]
        public void TestAclAuthorizationOnComputerTarget(string username, string computerName, AccessMask allowed, AccessMask denied, AccessMask expected)
        {
            IUser user = directory.GetUser(username);
            IComputer computer = directory.GetComputer(computerName);

            var options = SetupOptionsForComputerTarget(allowed, denied, computer, user);

            builder = new AuthorizationInformationBuilder(options, logger, powershell, cache, targetDataProvider, authorizationContextProvider, licenseManager);
            var result = builder.GetAuthorizationInformation(user, computer);

            Assert.AreEqual(expected, result.EffectiveAccess);
        }

        [TestCase(C.DEV_User1, C.DEV_PC1, C.Computers_AmsTesting_DevDN, AccessMask.LocalAdminPassword, AccessMask.None, AccessMask.LocalAdminPassword)]
        [TestCase(C.DEV_User1, C.DEV_PC1, C.AmsTesting_DevDN, AccessMask.LocalAdminPassword, AccessMask.None, AccessMask.LocalAdminPassword)]
        [TestCase(C.DEV_User1, C.DEV_PC1, C.DevDN, AccessMask.LocalAdminPassword, AccessMask.None, AccessMask.LocalAdminPassword)]
        public void TestAclAuthorizationOnOUTarget(string username, string computerName, string targetOU, AccessMask allowed, AccessMask denied, AccessMask expected)
        {
            IUser user = directory.GetUser(username);
            IComputer computer = directory.GetComputer(computerName);

            var options = SetupOptionsForOUTarget(allowed, denied, targetOU, user);

            builder = new AuthorizationInformationBuilder(options, logger, powershell, cache, targetDataProvider, authorizationContextProvider, licenseManager);
            var result = builder.GetAuthorizationInformation(user, computer);

            Assert.AreEqual(expected, result.EffectiveAccess);
        }


        [TestCase(C.Dev)]
        [TestCase(C.SubDev)]
        [TestCase(C.ExtDev)]
        public void GetMatchingTargetsForComputer(string targetDomain)
        {
            ISecurityPrincipal trustee = directory.GetPrincipal($"{targetDomain}\\user1");

            IComputer computer1 = directory.GetComputer($"{targetDomain}\\PC1");
            IComputer computer2 = directory.GetComputer($"{targetDomain}\\PC2");
            IGroup group1 = directory.GetGroup($"{targetDomain}\\G-DL-PC1");
            IGroup group2 = directory.GetGroup($"{targetDomain}\\G-DL-PC2");

            var namingContext = directory.TranslateName(targetDomain + "\\", DsNameFormat.Nt4Name, DsNameFormat.DistinguishedName);

            var t1 = CreateTarget(AccessMask.LocalAdminPassword, AccessMask.None, $"OU=Computers,OU=AMS Testing,{namingContext}", trustee);
            var t2 = CreateTarget(AccessMask.LocalAdminPassword, AccessMask.None, $"OU=AMS Testing,{namingContext}", trustee);
            var t3 = CreateTarget(AccessMask.LocalAdminPassword, AccessMask.None, $"{namingContext}", trustee);
            var t4 = CreateTarget(AccessMask.LocalAdminPassword, AccessMask.None, $"OU=JIT Groups,OU=AMS Testing,{namingContext}", trustee);
            var t5 = CreateTarget(AccessMask.LocalAdminPassword, AccessMask.None, computer1, trustee);
            var t6 = CreateTarget(AccessMask.LocalAdminPassword, AccessMask.None, computer2, trustee);
            var t7 = CreateTarget(AccessMask.LocalAdminPassword, AccessMask.None, group1, trustee);
            var t8 = CreateTarget(AccessMask.LocalAdminPassword, AccessMask.None, group2, trustee);

            var options = SetupOptions(t1, t2, t3, t4, t5, t6, t7, t8);

            builder = new AuthorizationInformationBuilder(options, logger, powershell, cache, targetDataProvider, authorizationContextProvider, licenseManager);

            CollectionAssert.AreEquivalent(new[] { t1, t2, t3, t5, t7 }, targetDataProvider.GetMatchingTargetsForComputer(computer1, options.Value.ComputerTargets));
        }

        [Test]
        public void ValidateTargetSortOrder()
        {
            ISecurityPrincipal trustee = directory.GetPrincipal(C.DEV_User1);
            IComputer computer1 = directory.GetComputer(C.DEV_PC1);

            var t1 = CreateTarget(AccessMask.LocalAdminPassword, AccessMask.None, C.AmsTesting_DevDN, trustee);
            var t2 = CreateTarget(AccessMask.LocalAdminPassword, AccessMask.None, C.DevDN, trustee);
            var t3 = CreateTarget(AccessMask.LocalAdminPassword, AccessMask.None, C.Computers_AmsTesting_DevDN, trustee);
            var t4 = CreateTarget(AccessMask.LocalAdminPassword, AccessMask.None, C.JitGroups_AmsTesting_DevDN, trustee);

            var options = SetupOptions(t1, t2, t3, t4);

            builder = new AuthorizationInformationBuilder(options, logger, powershell, cache, targetDataProvider, authorizationContextProvider, licenseManager);

            CollectionAssert.AreEqual(new[] { t3, t1, t2 }, targetDataProvider.GetMatchingTargetsForComputer(computer1, options.Value.ComputerTargets));
        }

        [TestCase(C.DEV_User1, C.DEV_User1, C.Dev)]
        [TestCase(C.DEV_G_UG_1, C.DEV_User1, C.Dev)]
        [TestCase(C.DEV_User1, C.DEV_User1, C.SubDev)]
        [TestCase(C.DEV_G_UG_1, C.DEV_User1, C.SubDev)]
        [TestCase(C.SUBDEV_User1, C.SUBDEV_User1, C.SubDev)]
        [TestCase(C.SUBDEV_G_UG_1, C.SUBDEV_User1, C.SubDev)]
        [TestCase(C.EXTDEV_User1, C.EXTDEV_User1, C.ExtDev)]
        [TestCase(C.EXTDEV_G_UG_1, C.EXTDEV_User1, C.ExtDev)]
        public void AllowTrusteeOnComputerTarget(string trusteeName, string requestorName, string targetDomain)
        {
            ISecurityPrincipal trustee = directory.GetPrincipal(trusteeName);
            IUser requestor = directory.GetUser(requestorName);

            IComputer computer1 = directory.GetComputer($"{targetDomain}\\PC1");
            IComputer computer2 = directory.GetComputer($"{targetDomain}\\PC2");
            IGroup group1 = directory.GetGroup($"{targetDomain}\\G-DL-PC1");
            IGroup group2 = directory.GetGroup($"{targetDomain}\\G-DL-PC2");

            var namingContext = directory.TranslateName(targetDomain + "\\", DsNameFormat.Nt4Name, DsNameFormat.DistinguishedName);

            var t1 = CreateTarget(AccessMask.None, AccessMask.None, $"OU=Computers,OU=AMS Testing,{namingContext}", trustee);
            var t2 = CreateTarget(AccessMask.None, AccessMask.None, $"OU=AMS Testing,{namingContext}", trustee);
            var t3 = CreateTarget(AccessMask.None, AccessMask.None, $"{namingContext}", trustee);
            var t4 = CreateTarget(AccessMask.None, AccessMask.None, $"OU=JIT Groups,OU=AMS Testing,{namingContext}", trustee);
            var t5 = CreateTarget(AccessMask.LocalAdminPassword, AccessMask.None, computer1, trustee);
            var t6 = CreateTarget(AccessMask.None, AccessMask.None, computer2, trustee);
            var t7 = CreateTarget(AccessMask.None, AccessMask.None, group1, trustee);
            var t8 = CreateTarget(AccessMask.None, AccessMask.None, group2, trustee);

            var options = SetupOptions(t1, t2, t3, t4, t5, t6, t7, t8);

            builder = new AuthorizationInformationBuilder(options, logger, powershell, cache, targetDataProvider, authorizationContextProvider, licenseManager);
            var result = builder.GetAuthorizationInformation(requestor, computer1);

            Assert.AreEqual(AccessMask.LocalAdminPassword, result.EffectiveAccess);

            CollectionAssert.AreEquivalent(new[] { t5 }, result.SuccessfulLapsTargets);
        }

        [TestCase(C.DEV_User1, C.DEV_User1, C.Dev)]
        [TestCase(C.DEV_G_UG_1, C.DEV_User1, C.Dev)]
        [TestCase(C.DEV_User1, C.DEV_User1, C.SubDev)]
        [TestCase(C.DEV_G_UG_1, C.DEV_User1, C.SubDev)]
        [TestCase(C.SUBDEV_User1, C.SUBDEV_User1, C.SubDev)]
        [TestCase(C.SUBDEV_G_UG_1, C.SUBDEV_User1, C.SubDev)]
        [TestCase(C.EXTDEV_User1, C.EXTDEV_User1, C.ExtDev)]
        [TestCase(C.EXTDEV_G_UG_1, C.EXTDEV_User1, C.ExtDev)]
        public void AllowTrusteeOnGroupTarget(string trusteeName, string requestorName, string targetDomain)
        {
            ISecurityPrincipal trustee = directory.GetPrincipal(trusteeName);
            IUser requestor = directory.GetUser(requestorName);

            IComputer computer1 = directory.GetComputer($"{targetDomain}\\PC1");
            IComputer computer2 = directory.GetComputer($"{targetDomain}\\PC2");
            IGroup group1 = directory.GetGroup($"{targetDomain}\\G-DL-PC1");
            IGroup group2 = directory.GetGroup($"{targetDomain}\\G-DL-PC2");

            var namingContext = directory.TranslateName(targetDomain + "\\", DsNameFormat.Nt4Name, DsNameFormat.DistinguishedName);

            var t1 = CreateTarget(AccessMask.None, AccessMask.None, $"OU=Computers,OU=AMS Testing,{namingContext}", trustee);
            var t2 = CreateTarget(AccessMask.None, AccessMask.None, $"OU=AMS Testing,{namingContext}", trustee);
            var t3 = CreateTarget(AccessMask.None, AccessMask.None, $"{namingContext}", trustee);
            var t4 = CreateTarget(AccessMask.None, AccessMask.None, $"OU=JIT Groups,OU=AMS Testing,{namingContext}", trustee);
            var t5 = CreateTarget(AccessMask.None, AccessMask.None, computer1, trustee);
            var t6 = CreateTarget(AccessMask.None, AccessMask.None, computer2, trustee);
            var t7 = CreateTarget(AccessMask.LocalAdminPassword, AccessMask.None, group1, trustee);
            var t8 = CreateTarget(AccessMask.None, AccessMask.None, group2, trustee);

            var options = SetupOptions(t1, t2, t3, t4, t5, t6, t7, t8);

            builder = new AuthorizationInformationBuilder(options, logger, powershell, cache, targetDataProvider, authorizationContextProvider, licenseManager);
            var result = builder.GetAuthorizationInformation(requestor, computer1);

            Assert.AreEqual(AccessMask.LocalAdminPassword, result.EffectiveAccess);

            CollectionAssert.AreEquivalent(new[] { t7 }, result.SuccessfulLapsTargets);
        }

        [TestCase(C.DEV_User1, C.DEV_User1, C.Dev)]
        [TestCase(C.DEV_G_UG_1, C.DEV_User1, C.Dev)]
        [TestCase(C.DEV_User1, C.DEV_User1, C.SubDev)]
        [TestCase(C.DEV_G_UG_1, C.DEV_User1, C.SubDev)]
        [TestCase(C.SUBDEV_User1, C.SUBDEV_User1, C.SubDev)]
        [TestCase(C.SUBDEV_G_UG_1, C.SUBDEV_User1, C.SubDev)]
        [TestCase(C.EXTDEV_User1, C.EXTDEV_User1, C.ExtDev)]
        [TestCase(C.EXTDEV_G_UG_1, C.EXTDEV_User1, C.ExtDev)]
        public void AllowTrusteeOnOUTarget(string trusteeName, string requestorName, string targetDomain)
        {
            ISecurityPrincipal trustee = directory.GetPrincipal(trusteeName);
            IUser requestor = directory.GetUser(requestorName);

            IComputer computer1 = directory.GetComputer($"{targetDomain}\\PC1");
            IComputer computer2 = directory.GetComputer($"{targetDomain}\\PC2");
            IGroup group1 = directory.GetGroup($"{targetDomain}\\G-DL-PC1");
            IGroup group2 = directory.GetGroup($"{targetDomain}\\G-DL-PC2");

            var namingContext = directory.TranslateName(targetDomain + "\\", DsNameFormat.Nt4Name, DsNameFormat.DistinguishedName);

            var t1 = CreateTarget(AccessMask.None, AccessMask.None, $"OU=Computers,OU=AMS Testing,{namingContext}", trustee);
            var t2 = CreateTarget(AccessMask.None, AccessMask.None, $"OU=AMS Testing,{namingContext}", trustee);
            var t3 = CreateTarget(AccessMask.LocalAdminPassword, AccessMask.None, $"{namingContext}", trustee);
            var t4 = CreateTarget(AccessMask.None, AccessMask.None, $"OU=JIT Groups,OU=AMS Testing,{namingContext}", trustee);
            var t5 = CreateTarget(AccessMask.None, AccessMask.None, computer1, trustee);
            var t6 = CreateTarget(AccessMask.None, AccessMask.None, computer2, trustee);
            var t7 = CreateTarget(AccessMask.None, AccessMask.None, group1, trustee);
            var t8 = CreateTarget(AccessMask.None, AccessMask.None, group2, trustee);

            var options = SetupOptions(t1, t2, t3, t4, t5, t6, t7, t8);

            builder = new AuthorizationInformationBuilder(options, logger, powershell, cache, targetDataProvider, authorizationContextProvider, licenseManager);
            var result = builder.GetAuthorizationInformation(requestor, computer1);

            Assert.AreEqual(AccessMask.LocalAdminPassword, result.EffectiveAccess);

            CollectionAssert.AreEquivalent(new[] { t3 }, result.SuccessfulLapsTargets);
        }

        [TestCase(C.DEV_User1, C.DEV_User1, C.Dev)]
        [TestCase(C.DEV_G_UG_1, C.DEV_User1, C.Dev)]
        [TestCase(C.DEV_User1, C.DEV_User1, C.SubDev)]
        [TestCase(C.DEV_G_UG_1, C.DEV_User1, C.SubDev)]
        [TestCase(C.SUBDEV_User1, C.SUBDEV_User1, C.SubDev)]
        [TestCase(C.SUBDEV_G_UG_1, C.SUBDEV_User1, C.SubDev)]
        [TestCase(C.EXTDEV_User1, C.EXTDEV_User1, C.ExtDev)]
        [TestCase(C.EXTDEV_G_UG_1, C.EXTDEV_User1, C.ExtDev)]
        public void DenyTrusteeOnComputerTarget(string trusteeName, string requestorName, string targetDomain)
        {
            ISecurityPrincipal trustee = directory.GetPrincipal(trusteeName);
            IUser requestor = directory.GetUser(requestorName);

            IComputer computer1 = directory.GetComputer($"{targetDomain}\\PC1");
            IComputer computer2 = directory.GetComputer($"{targetDomain}\\PC2");
            IGroup group1 = directory.GetGroup($"{targetDomain}\\G-DL-PC1");
            IGroup group2 = directory.GetGroup($"{targetDomain}\\G-DL-PC2");

            var namingContext = directory.TranslateName(targetDomain + "\\", DsNameFormat.Nt4Name, DsNameFormat.DistinguishedName);

            var t1 = CreateTarget(AccessMask.LocalAdminPassword, AccessMask.None, $"OU=Computers,OU=AMS Testing,{namingContext}", trustee);
            var t2 = CreateTarget(AccessMask.LocalAdminPassword, AccessMask.None, $"OU=AMS Testing,{namingContext}", trustee);
            var t3 = CreateTarget(AccessMask.LocalAdminPassword, AccessMask.None, $"{namingContext}", trustee);
            var t4 = CreateTarget(AccessMask.LocalAdminPassword, AccessMask.None, $"OU=JIT Groups,OU=AMS Testing,{namingContext}", trustee);
            var t5 = CreateTarget(AccessMask.None, AccessMask.LocalAdminPassword, computer1, trustee);
            var t6 = CreateTarget(AccessMask.LocalAdminPassword, AccessMask.None, computer2, trustee);
            var t7 = CreateTarget(AccessMask.LocalAdminPassword, AccessMask.None, group1, trustee);
            var t8 = CreateTarget(AccessMask.LocalAdminPassword, AccessMask.None, group2, trustee);

            var options = SetupOptions(t1, t2, t3, t4, t5, t6, t7, t8);

            builder = new AuthorizationInformationBuilder(options, logger, powershell, cache, targetDataProvider, authorizationContextProvider, licenseManager);
            var result = builder.GetAuthorizationInformation(requestor, computer1);
            Assert.AreEqual(AccessMask.None, result.EffectiveAccess);
            CollectionAssert.AreEquivalent(new[] { t1, t2, t3, t7 }, result.SuccessfulLapsTargets);
        }

        [TestCase(C.DEV_User1, C.DEV_User1, C.Dev)]
        [TestCase(C.DEV_G_UG_1, C.DEV_User1, C.Dev)]
        [TestCase(C.DEV_User1, C.DEV_User1, C.SubDev)]
        [TestCase(C.DEV_G_UG_1, C.DEV_User1, C.SubDev)]
        [TestCase(C.SUBDEV_User1, C.SUBDEV_User1, C.SubDev)]
        [TestCase(C.SUBDEV_G_UG_1, C.SUBDEV_User1, C.SubDev)]
        [TestCase(C.EXTDEV_User1, C.EXTDEV_User1, C.ExtDev)]
        [TestCase(C.EXTDEV_G_UG_1, C.EXTDEV_User1, C.ExtDev)]
        public void DenyTrusteeOnOUTarget(string trusteeName, string requestorName, string targetDomain)
        {
            ISecurityPrincipal trustee = directory.GetPrincipal(trusteeName);
            IUser requestor = directory.GetUser(requestorName);

            IComputer computer1 = directory.GetComputer($"{targetDomain}\\PC1");
            IComputer computer2 = directory.GetComputer($"{targetDomain}\\PC2");
            IGroup group1 = directory.GetGroup($"{targetDomain}\\G-DL-PC1");
            IGroup group2 = directory.GetGroup($"{targetDomain}\\G-DL-PC2");

            var namingContext = directory.TranslateName(targetDomain + "\\", DsNameFormat.Nt4Name, DsNameFormat.DistinguishedName);

            var t1 = CreateTarget(AccessMask.LocalAdminPassword, AccessMask.None, $"OU=Computers,OU=AMS Testing,{namingContext}", trustee);
            var t2 = CreateTarget(AccessMask.LocalAdminPassword, AccessMask.LocalAdminPassword, $"OU=AMS Testing,{namingContext}", trustee);
            var t3 = CreateTarget(AccessMask.LocalAdminPassword, AccessMask.None, $"{namingContext}", trustee);
            var t4 = CreateTarget(AccessMask.LocalAdminPassword, AccessMask.None, $"OU=JIT Groups,OU=AMS Testing,{namingContext}", trustee);
            var t5 = CreateTarget(AccessMask.LocalAdminPassword, AccessMask.None, computer1, trustee);
            var t6 = CreateTarget(AccessMask.LocalAdminPassword, AccessMask.None, computer2, trustee);
            var t7 = CreateTarget(AccessMask.LocalAdminPassword, AccessMask.None, group1, trustee);
            var t8 = CreateTarget(AccessMask.LocalAdminPassword, AccessMask.None, group2, trustee);

            var options = SetupOptions(t1, t2, t3, t4, t5, t6, t7, t8);

            builder = new AuthorizationInformationBuilder(options, logger, powershell, cache, targetDataProvider, authorizationContextProvider, licenseManager);
            var result = builder.GetAuthorizationInformation(requestor, computer1);
            CollectionAssert.AreEquivalent(new[] { t1, t3, t5, t7 }, result.SuccessfulLapsTargets);
            Assert.AreEqual(AccessMask.None, result.EffectiveAccess);
        }

        [TestCase(C.DEV_User1, C.DEV_User1, C.Dev)]
        [TestCase(C.DEV_G_UG_1, C.DEV_User1, C.Dev)]
        [TestCase(C.DEV_User1, C.DEV_User1, C.SubDev)]
        [TestCase(C.DEV_G_UG_1, C.DEV_User1, C.SubDev)]
        [TestCase(C.SUBDEV_User1, C.SUBDEV_User1, C.SubDev)]
        [TestCase(C.SUBDEV_G_UG_1, C.SUBDEV_User1, C.SubDev)]
        [TestCase(C.EXTDEV_User1, C.EXTDEV_User1, C.ExtDev)]
        [TestCase(C.EXTDEV_G_UG_1, C.EXTDEV_User1, C.ExtDev)]
        public void DenyTrusteeOnGroupTarget(string trusteeName, string requestorName, string targetDomain)
        {
            ISecurityPrincipal trustee = directory.GetPrincipal(trusteeName);
            IUser requestor = directory.GetUser(requestorName);

            IComputer computer1 = directory.GetComputer($"{targetDomain}\\PC1");
            IComputer computer2 = directory.GetComputer($"{targetDomain}\\PC2");
            IGroup group1 = directory.GetGroup($"{targetDomain}\\G-DL-PC1");
            IGroup group2 = directory.GetGroup($"{targetDomain}\\G-DL-PC2");

            var namingContext = directory.TranslateName(targetDomain + "\\", DsNameFormat.Nt4Name, DsNameFormat.DistinguishedName);

            var t1 = CreateTarget(AccessMask.LocalAdminPassword, AccessMask.None, $"OU=Computers,OU=AMS Testing,{namingContext}", trustee);
            var t2 = CreateTarget(AccessMask.LocalAdminPassword, AccessMask.None, $"OU=AMS Testing,{namingContext}", trustee);
            var t3 = CreateTarget(AccessMask.LocalAdminPassword, AccessMask.None, $"{namingContext}", trustee);
            var t4 = CreateTarget(AccessMask.LocalAdminPassword, AccessMask.None, $"OU=JIT Groups,OU=AMS Testing,{namingContext}", trustee);
            var t5 = CreateTarget(AccessMask.LocalAdminPassword, AccessMask.None, computer1, trustee);
            var t6 = CreateTarget(AccessMask.LocalAdminPassword, AccessMask.None, computer2, trustee);
            var t7 = CreateTarget(AccessMask.None, AccessMask.LocalAdminPassword, group1, trustee);
            var t8 = CreateTarget(AccessMask.LocalAdminPassword, AccessMask.None, group2, trustee);

            var options = SetupOptions(t1, t2, t3, t4, t5, t6, t7, t8);

            builder = new AuthorizationInformationBuilder(options, logger, powershell, cache, targetDataProvider, authorizationContextProvider, licenseManager);
            var result = builder.GetAuthorizationInformation(requestor, computer1);
            CollectionAssert.AreEquivalent(new[] { t1, t2, t5, t3 }, result.SuccessfulLapsTargets);
            Assert.AreEqual(AccessMask.None, result.EffectiveAccess);
        }


        [TestCase(C.DEV_User1, C.DEV_User1, C.DEV_PC1)]
        [TestCase(C.DEV_User1, C.DEV_User1, C.SUBDEV_PC1)]
        [TestCase(C.DEV_User1, C.DEV_User1, C.EXTDEV_PC1)]
        [TestCase(C.SUBDEV_User1, C.SUBDEV_User1, C.DEV_PC1)]
        [TestCase(C.SUBDEV_User1, C.SUBDEV_User1, C.SUBDEV_PC1)]
        [TestCase(C.SUBDEV_User1, C.SUBDEV_User1, C.EXTDEV_PC1)]
        [TestCase(C.EXTDEV_User1, C.EXTDEV_User1, C.EXTDEV_PC1)]
        public void UserCanAccessComputer(string requestorName, string trusteeName, string computerName)
        {
            IUser requestor = directory.GetUser(requestorName);
            ISecurityPrincipal trustee = directory.GetPrincipal(trusteeName);
            IComputer computer = directory.GetComputer(computerName);

            var t1 = CreateTarget(AccessMask.LocalAdminPassword, AccessMask.None, computer, trustee);

            var options = SetupOptions(t1);

            builder = new AuthorizationInformationBuilder(options, logger, powershell, cache, targetDataProvider, authorizationContextProvider, licenseManager);
            var result = builder.GetAuthorizationInformation(requestor, computer);
            CollectionAssert.AreEquivalent(new[] { t1 }, result.SuccessfulLapsTargets);
            Assert.AreEqual(AccessMask.LocalAdminPassword, result.EffectiveAccess);
        }

        // IDMDEV1\\user1 can access PCs in all domains via global groups in their home domain
        [TestCase(C.DEV_User1, C.DEV_G_GG_1, C.DEV_PC1)]
        [TestCase(C.DEV_User1, C.DEV_G_GG_1, C.SUBDEV_PC1)]
        [TestCase(C.DEV_User1, C.DEV_G_GG_1, C.EXTDEV_PC1)]

        // IDMDEV1\\user1 can access PCs in all domains via universal groups in their own forest
        [TestCase(C.DEV_User1, C.DEV_G_UG_1, C.DEV_PC1)]
        [TestCase(C.DEV_User1, C.DEV_G_UG_1, C.SUBDEV_PC1)]
        [TestCase(C.DEV_User1, C.DEV_G_UG_1, C.EXTDEV_PC1)]
        [TestCase(C.DEV_User1, C.SUBDEV_G_UG_1, C.DEV_PC1)]
        [TestCase(C.DEV_User1, C.SUBDEV_G_UG_1, C.SUBDEV_PC1)]
        [TestCase(C.DEV_User1, C.SUBDEV_G_UG_1, C.EXTDEV_PC1)]

        // IDMDEV1\\user1 can access PCs in their own forest via domain local groups in each domain
        [TestCase(C.DEV_User1, C.DEV_G_DL_1, C.DEV_PC1)]
        [TestCase(C.DEV_User1, C.SUBDEV_G_DL_1, C.SUBDEV_PC1)]


        // SUBDEV1\\user1 can access PCs in all domains via global groups in their home domain
        [TestCase(C.SUBDEV_User1, C.SUBDEV_G_GG_1, C.DEV_PC1)]
        [TestCase(C.SUBDEV_User1, C.SUBDEV_G_GG_1, C.SUBDEV_PC1)]
        [TestCase(C.SUBDEV_User1, C.SUBDEV_G_GG_1, C.EXTDEV_PC1)]

        // SUBDEV1\\user1 can access PCs in all domains via universal groups in their own forest
        [TestCase(C.SUBDEV_User1, C.DEV_G_UG_1, C.DEV_PC1)]
        [TestCase(C.SUBDEV_User1, C.DEV_G_UG_1, C.SUBDEV_PC1)]
        [TestCase(C.SUBDEV_User1, C.DEV_G_UG_1, C.EXTDEV_PC1)]
        [TestCase(C.SUBDEV_User1, C.SUBDEV_G_UG_1, C.DEV_PC1)]
        [TestCase(C.SUBDEV_User1, C.SUBDEV_G_UG_1, C.SUBDEV_PC1)]
        [TestCase(C.SUBDEV_User1, C.SUBDEV_G_UG_1, C.EXTDEV_PC1)]

        // SUBDEV1\\user1 can access PCs in their own forest via domain local groups in each domain
        [TestCase(C.SUBDEV_User1, C.DEV_G_DL_1, C.DEV_PC1)]
        [TestCase(C.SUBDEV_User1, C.SUBDEV_G_DL_1, C.SUBDEV_PC1)]

        // EXTDEV1\\user1 can access PCs via global groups only in their home domain
        [TestCase(C.EXTDEV_User1, C.EXTDEV_G_GG_1, C.EXTDEV_PC1)]

        // EXTDEV1\\user1 can access PCs via universal groups in their home domain forest
        [TestCase(C.EXTDEV_User1, C.EXTDEV_G_UG_1, C.EXTDEV_PC1)]

        // EXTDEV1\\user1 can access PCs in their own forest via domain local groups
        [TestCase(C.EXTDEV_User1, C.EXTDEV_G_DL_1, C.EXTDEV_PC1)]

        public void GroupCanAccessComputer(string requestorName, string trusteeName, string computerName)
        {
            IUser requestor = directory.GetUser(requestorName);
            ISecurityPrincipal trustee = directory.GetPrincipal(trusteeName);
            IComputer computer = directory.GetComputer(computerName);

            var t1 = CreateTarget(AccessMask.LocalAdminPassword, AccessMask.None, computer, trustee);

            var options = SetupOptions(t1);

            builder = new AuthorizationInformationBuilder(options, logger, powershell, cache, targetDataProvider, authorizationContextProvider, licenseManager);
            var result = builder.GetAuthorizationInformation(requestor, computer);

            CollectionAssert.AreEquivalent(new[] { t1 }, result.SuccessfulLapsTargets);
            Assert.AreEqual(AccessMask.LocalAdminPassword, result.EffectiveAccess);
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