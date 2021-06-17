using System;
using System.Collections.Generic;
using System.Threading;
using Lithnet.AccessManager.Server.Configuration;
using Lithnet.AccessManager.Server.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using C = Lithnet.AccessManager.Test.TestEnvironmentConstants;

namespace Lithnet.AccessManager.Server.Test
{
    public class JitAccessProviderTests
    {
        private IDirectory directory;

        private IDiscoveryServices discoveryServices;

        private JitAccessProvider provider;

        private ILogger<JitAccessProvider> logger;

        private JitConfigurationOptions options;

        public JitAccessProviderTests()
        {
        }

        [SetUp()]
        public void TestInitialize()
        {
            this.discoveryServices = new DiscoveryServices(Global.LogFactory.CreateLogger<DiscoveryServices>());
            directory = new ActiveDirectory(discoveryServices);
            options = new JitConfigurationOptions
            {
                DynamicGroupMappings = new List<JitDynamicGroupMapping>()
            };

            logger = Global.LogFactory.CreateLogger<JitAccessProvider>();
        }

        [TestCase(C.DynamicJitGroups_AmsTesting_DevDN, C.DEV_PC1,C.DEV_JIT_PC1, C.DEV_User1)]
        [TestCase(C.DynamicJitGroups_AmsTesting_DevDN, C.DEV_PC1,C.DEV_JIT_PC1, C.SUBDEV_User1)]
        [TestCase(C.DynamicJitGroups_AmsTesting_SubDevDN, C.SUBDEV_PC1, C.SUBDEV_JIT_PC1, C.SUBDEV_User1)]
        [TestCase(C.DynamicJitGroups_AmsTesting_SubDevDN, C.SUBDEV_PC1, C.SUBDEV_JIT_PC1, C.DEV_User1)]
        [TestCase(C.DynamicJitGroups_AmsTesting_ExtDevDN, C.EXTDEV_PC1, C.EXTDEV_JIT_PC1, C.EXTDEV_User1)]
        [TestCase(C.DynamicJitGroups_AmsTesting_ExtDevDN, C.EXTDEV_PC1, C.EXTDEV_JIT_PC1, C.DEV_User1)]
        [TestCase(C.DynamicJitGroups_AmsTesting_ExtDevDN, C.EXTDEV_PC1, C.EXTDEV_JIT_PC1, C.SUBDEV_User1)]
        public void CreateDynamicGroup(string groupou, string computerName, string jitGroupName, string userName)
        {
            string groupname = Guid.NewGuid().ToString();
            string fqGroupName = $"{jitGroupName.Split('\\')[0]}\\{groupname}";

            IGroup jitGroup = directory.GetGroup(jitGroupName);
            jitGroup.RemoveMembers();
            IUser user = directory.GetUser(userName);
            IActiveDirectoryComputer computer = directory.GetComputer(computerName);

            options.DynamicGroupMappings.Add(new JitDynamicGroupMapping()
            {
                Domain = jitGroup.Sid.AccountDomainSid.ToString(),
                GroupOU = groupou,
                GroupNameTemplate = groupname
            });

            this.provider = new JitAccessProvider(directory, logger, this.GetOptions(), discoveryServices);

            provider.GrantJitAccessDynamicGroup(jitGroup, user, null, false, TimeSpan.FromMinutes(1), out _);
            Thread.Sleep(TimeSpan.FromSeconds(20));

            IGroup ttlGroup = directory.GetGroup(fqGroupName);
            Assert.IsNotNull(ttlGroup);
            Assert.AreEqual(groupname, ttlGroup.SamAccountName);

            directory.IsSidInPrincipalToken(ttlGroup.Sid, jitGroup.Sid);
            directory.IsSidInPrincipalToken(user.Sid, ttlGroup.Sid);
            directory.IsSidInPrincipalToken(user.Sid, jitGroup.Sid);
        }

        [TestCase(C.DynamicJitGroups_AmsTesting_DevDN, C.DEV_PC1,C.DEV_JIT_PC1, C.DEV_User1)]
        [TestCase(C.DynamicJitGroups_AmsTesting_ExtDevDN, C.EXTDEV_PC1, C.EXTDEV_JIT_PC1, C.EXTDEV_User1)]
        [TestCase(C.DynamicJitGroups_AmsTesting_SubDevDN, C.SUBDEV_PC1, C.SUBDEV_JIT_PC1, C.SUBDEV_User1)]
        public void TestDynamicGroupAccessExtensionNotAllowed(string groupou, string computerName, string jitGroupName, string userName)
        {
            string groupname = Guid.NewGuid().ToString();
            string fqGroupName = $"{jitGroupName.Split('\\')[0]}\\{groupname}";

            IGroup jitGroup = directory.GetGroup(jitGroupName);
            jitGroup.RemoveMembers();
            IUser user = directory.GetUser(userName);
            IActiveDirectoryComputer computer = directory.GetComputer(computerName);

            options.DynamicGroupMappings.Add(new JitDynamicGroupMapping()
            {
                Domain = jitGroup.Sid.AccountDomainSid.ToString(),
                GroupOU = groupou,
                GroupNameTemplate = groupname
            });

            this.provider = new JitAccessProvider(directory, logger, this.GetOptions(), discoveryServices);

            TimeSpan allowedAccess = provider.GrantJitAccessDynamicGroup(jitGroup, user, null, false, TimeSpan.FromMinutes(1), out _);

            Assert.AreEqual(1, allowedAccess.TotalMinutes);

            Thread.Sleep(TimeSpan.FromSeconds(20));

            TimeSpan allowedAccess2 = provider.GrantJitAccessDynamicGroup(jitGroup, user, null, false, TimeSpan.FromMinutes(1), out _);

            Assert.LessOrEqual(allowedAccess2.TotalSeconds, 50);
        }

        [TestCase(C.DynamicJitGroups_AmsTesting_DevDN, C.DEV_PC1,C.DEV_JIT_PC1, C.DEV_User1)]
        [TestCase(C.DynamicJitGroups_AmsTesting_ExtDevDN, C.EXTDEV_PC1, C.EXTDEV_JIT_PC1, C.EXTDEV_User1)]
        [TestCase(C.DynamicJitGroups_AmsTesting_SubDevDN, C.SUBDEV_PC1, C.SUBDEV_JIT_PC1, C.SUBDEV_User1)]
        public void TestDynamicGroupAccessExtensionAllowed(string groupou, string computerName, string jitGroupName, string userName)
        {
            string groupname = Guid.NewGuid().ToString();
            string fqGroupName = $"{jitGroupName.Split('\\')[0]}\\{groupname}";

            IGroup jitGroup = directory.GetGroup(jitGroupName);
            jitGroup.RemoveMembers();
            IUser user = directory.GetUser(userName);
            IActiveDirectoryComputer computer = directory.GetComputer(computerName);

            options.DynamicGroupMappings.Add(new JitDynamicGroupMapping()
            {
                Domain = jitGroup.Sid.AccountDomainSid.ToString(),
                GroupOU = groupou,
                GroupNameTemplate = groupname
            });

            this.provider = new JitAccessProvider(directory, logger, this.GetOptions(), discoveryServices);

            TimeSpan allowedAccess = provider.GrantJitAccessDynamicGroup(jitGroup, user, null, true, TimeSpan.FromMinutes(1), out _);

            Assert.AreEqual(1, allowedAccess.TotalMinutes);

            Thread.Sleep(TimeSpan.FromSeconds(20));

            TimeSpan allowedAccess2 = provider.GrantJitAccessDynamicGroup(jitGroup, user, null, true, TimeSpan.FromMinutes(1), out _);

            Assert.AreEqual(allowedAccess2.TotalSeconds, 60);
        }

        [TestCase(C.DynamicJitGroups_AmsTesting_DevDN, C.DEV_PC1,C.DEV_JIT_PC1, C.DEV_User1)]
        [TestCase(C.DynamicJitGroups_AmsTesting_DevDN, C.DEV_PC1,C.DEV_JIT_PC1, C.SUBDEV_User1)]
        [TestCase(C.DynamicJitGroups_AmsTesting_SubDevDN, C.SUBDEV_PC1, C.SUBDEV_JIT_PC1, C.SUBDEV_User1)]
        [TestCase(C.DynamicJitGroups_AmsTesting_SubDevDN, C.SUBDEV_PC1, C.SUBDEV_JIT_PC1, C.DEV_User1)]
        [TestCase(C.DynamicJitGroups_AmsTesting_ExtDevDN, C.EXTDEV_PC1, C.EXTDEV_JIT_PC1, C.EXTDEV_User1)]
        [TestCase(C.DynamicJitGroups_AmsTesting_ExtDevDN, C.EXTDEV_PC1, C.EXTDEV_JIT_PC1, C.DEV_User1)]
        [TestCase(C.DynamicJitGroups_AmsTesting_ExtDevDN, C.EXTDEV_PC1, C.EXTDEV_JIT_PC1, C.SUBDEV_User1)]
        public void TestDynamicGroupAccessUndo(string groupou, string computerName, string jitGroupName, string userName)
        {
            string groupname = Guid.NewGuid().ToString();
            string fqGroupName = $"{jitGroupName.Split('\\')[0]}\\{groupname}";

            IGroup jitGroup = directory.GetGroup(jitGroupName);
            jitGroup.RemoveMembers();
            IUser user = directory.GetUser(userName);
            IActiveDirectoryComputer computer = directory.GetComputer(computerName);

            options.DynamicGroupMappings.Add(new JitDynamicGroupMapping()
            {
                Domain = jitGroup.Sid.AccountDomainSid.ToString(),
                GroupOU = groupou,
                GroupNameTemplate = groupname
            });

            this.provider = new JitAccessProvider(directory, logger, this.GetOptions(), discoveryServices);

            provider.GrantJitAccessDynamicGroup(jitGroup, user, null, true, TimeSpan.FromMinutes(1), out Action undo);

            Thread.Sleep(TimeSpan.FromSeconds(20));

            Assert.IsNotNull(this.directory.GetGroup(fqGroupName));
            undo();

            Thread.Sleep(TimeSpan.FromSeconds(20));
            Assert.IsFalse(this.directory.TryGetGroup(fqGroupName, out _));
        }


        [TestCase(C.DEV_JIT_PC1, C.DEV_PC1, C.DEV_User1)]
        [TestCase(C.SUBDEV_JIT_PC1, C.SUBDEV_PC1, C.SUBDEV_User1)]
        [TestCase(C.DEV_JIT_PC1, C.DEV_PC1, C.SUBDEV_User1)]
        [TestCase(C.SUBDEV_JIT_PC1, C.SUBDEV_PC1, C.DEV_User1)]
        [TestCase(C.EXTDEV_JIT_PC1, C.EXTDEV_PC1, C.SUBDEV_User1)]
        [TestCase(C.EXTDEV_JIT_PC1, C.EXTDEV_PC1, C.EXTDEV_User1)]
        [TestCase(C.EXTDEV_JIT_PC1, C.EXTDEV_PC1, C.DEV_User1)]
        public void TestPamGroupAccessExtensionAllowed(string jitGroupName, string computerName, string userName)
        {
            IGroup jitGroup = directory.GetGroup(jitGroupName);
            jitGroup.RemoveMembers();
            IUser user = directory.GetUser(userName);
            IActiveDirectoryComputer computer = directory.GetComputer(computerName);

            this.provider = new JitAccessProvider(directory, logger, this.GetOptions(), discoveryServices);

            TimeSpan allowedAccess = provider.GrantJitAccessPam(jitGroup, user, null, true, TimeSpan.FromMinutes(1), out _);
            TimeSpan? actualTtl = jitGroup.GetMemberTtl(user);

            Assert.AreEqual(1, allowedAccess.TotalMinutes);
            Assert.IsNotNull(actualTtl);
            Assert.LessOrEqual(actualTtl.Value.TotalSeconds, 60);

            Thread.Sleep(TimeSpan.FromSeconds(10));

            TimeSpan allowedAccess2 = provider.GrantJitAccessPam(jitGroup, user, null, true, TimeSpan.FromMinutes(2), out _);

            actualTtl = jitGroup.GetMemberTtl(user);

            Assert.AreEqual(2, allowedAccess2.TotalMinutes);
            Assert.IsNotNull(actualTtl);
            Assert.LessOrEqual(actualTtl.Value.TotalSeconds, 3600);
        }


        [TestCase(C.DEV_JIT_PC1, C.DEV_PC1, C.DEV_User1)]
        [TestCase(C.SUBDEV_JIT_PC1, C.SUBDEV_PC1, C.SUBDEV_User1)]
        [TestCase(C.DEV_JIT_PC1, C.DEV_PC1, C.SUBDEV_User1)]
        [TestCase(C.SUBDEV_JIT_PC1, C.SUBDEV_PC1, C.DEV_User1)]
        [TestCase(C.EXTDEV_JIT_PC1, C.EXTDEV_PC1, C.SUBDEV_User1)]
        [TestCase(C.EXTDEV_JIT_PC1, C.EXTDEV_PC1, C.EXTDEV_User1)]
        [TestCase(C.EXTDEV_JIT_PC1, C.EXTDEV_PC1, C.DEV_User1)]
        public void TestPamGroupAccessExtensionNotAllowed(string jitGroupName, string computerName, string userName)
        {
            IGroup jitGroup = directory.GetGroup(jitGroupName);
            jitGroup.RemoveMembers();
            IUser user = directory.GetUser(userName);
            IActiveDirectoryComputer computer = directory.GetComputer(computerName);

            this.provider = new JitAccessProvider(directory, logger, this.GetOptions(), discoveryServices);

            TimeSpan allowedAccess = provider.GrantJitAccessPam(jitGroup, user, null, false, TimeSpan.FromMinutes(1), out _);
            TimeSpan? actualTtl = jitGroup.GetMemberTtl(user);

            Assert.AreEqual(1, allowedAccess.TotalMinutes);
            Assert.IsNotNull(actualTtl);
            Assert.LessOrEqual(actualTtl.Value.TotalSeconds, 60);

            Thread.Sleep(TimeSpan.FromSeconds(10));

            TimeSpan allowedAccess2 = provider.GrantJitAccessPam(jitGroup, user, null, false, TimeSpan.FromMinutes(2), out _);

            actualTtl = jitGroup.GetMemberTtl(user);

            Assert.IsNotNull(actualTtl);
            Assert.LessOrEqual(actualTtl.Value.TotalSeconds, 60);
            Assert.LessOrEqual(allowedAccess2.TotalSeconds, 60);
        }

        [TestCase(C.DEV_JIT_PC1, C.DEV_PC1, C.DEV_User1)]
        [TestCase(C.SUBDEV_JIT_PC1, C.SUBDEV_PC1, C.SUBDEV_User1)]
        [TestCase(C.DEV_JIT_PC1, C.DEV_PC1, C.SUBDEV_User1)]
        [TestCase(C.SUBDEV_JIT_PC1, C.SUBDEV_PC1, C.DEV_User1)]
        [TestCase(C.EXTDEV_JIT_PC1, C.EXTDEV_PC1, C.SUBDEV_User1)]
        [TestCase(C.EXTDEV_JIT_PC1, C.EXTDEV_PC1, C.EXTDEV_User1)]
        [TestCase(C.EXTDEV_JIT_PC1, C.EXTDEV_PC1, C.DEV_User1)]
        public void TestPamGroupAccessUndo(string jitGroupName, string computerName, string userName)
        {
            IGroup jitGroup = directory.GetGroup(jitGroupName);
            jitGroup.RemoveMembers();
            IUser user = directory.GetUser(userName);
            IActiveDirectoryComputer computer = directory.GetComputer(computerName);

            this.provider = new JitAccessProvider(directory, logger, this.GetOptions(), discoveryServices);

            TimeSpan allowedAccess = provider.GrantJitAccessPam(jitGroup, user, null, false, TimeSpan.FromMinutes(1), out Action undo);
            TimeSpan? actualTtl = jitGroup.GetMemberTtl(user);

            Assert.AreEqual(1, allowedAccess.TotalMinutes);
            Assert.IsNotNull(actualTtl);
            Assert.LessOrEqual(actualTtl.Value.TotalSeconds, 60);

            undo();
            Assert.IsNull(jitGroup.GetMemberTtl(user));
        }


        [TestCase(C.DEV_JIT_PC1, C.DEV_PC1, C.DEV_User1)]
        public void ThrowOnNoMappingForDomain(string jitGroupName, string computerName, string userName)
        {
            IGroup jitGroup = directory.GetGroup(jitGroupName);
            jitGroup.RemoveMembers();
            IUser user = directory.GetUser(userName);
            IActiveDirectoryComputer computer = directory.GetComputer(computerName);

            this.provider = new JitAccessProvider(directory, logger, this.GetOptions(), discoveryServices);

            Assert.Throws<NoDynamicGroupMappingForDomainException>(() => provider.GrantJitAccessDynamicGroup(jitGroup, user, null, false, TimeSpan.FromMinutes(1), out _));
        }

        [TestCase(C.DEV_JIT_PC1, C.DEV_PC1, C.DEV_User1)]
        [TestCase(C.SUBDEV_JIT_PC1, C.SUBDEV_PC1, C.SUBDEV_User1)]
        [TestCase(C.DEV_JIT_PC1, C.DEV_PC1, C.SUBDEV_User1)]
        [TestCase(C.SUBDEV_JIT_PC1, C.SUBDEV_PC1, C.DEV_User1)]
        [TestCase(C.EXTDEV_JIT_PC1, C.EXTDEV_PC1, C.SUBDEV_User1)]
        [TestCase(C.EXTDEV_JIT_PC1, C.EXTDEV_PC1, C.EXTDEV_User1)]
        [TestCase(C.EXTDEV_JIT_PC1, C.EXTDEV_PC1, C.DEV_User1)]
        public void AddUserToGroupPam(string jitGroupName, string computerName, string userName)
        {
            IGroup jitGroup = directory.GetGroup(jitGroupName);
            jitGroup.RemoveMembers();
            IUser user = directory.GetUser(userName);
            IActiveDirectoryComputer computer = directory.GetComputer(computerName);

            this.provider = new JitAccessProvider(directory, logger, this.GetOptions(), discoveryServices);

            provider.GrantJitAccessPam(jitGroup, user, null, false, TimeSpan.FromMinutes(1), out _);

            directory.IsSidInPrincipalToken(user.Sid, jitGroup.Sid);
        }

        private IOptionsSnapshot<JitConfigurationOptions> GetOptions()
        {
            Mock<IOptionsSnapshot<JitConfigurationOptions>> ioptions = new Mock<IOptionsSnapshot<JitConfigurationOptions>>();
            ioptions.SetupGet(t => t.Value).Returns(options);

            return ioptions.Object;
        }
    }
}