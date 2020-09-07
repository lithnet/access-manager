using System;
using System.Collections.Generic;
using System.Threading;
using Lithnet.AccessManager.Server.Configuration;
using Lithnet.AccessManager.Server.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;

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

        [TestCase("OU=Dynamic JIT Groups,OU=LAPS Testing,DC=IDMDEV1,DC=LOCAL", "IDMDEV1\\PC1$", "IDMDEV1\\JIT-PC1", "IDMDEV1\\user1")]
        [TestCase("OU=Dynamic JIT Groups,OU=LAPS Testing,DC=IDMDEV1,DC=LOCAL", "IDMDEV1\\PC1$", "IDMDEV1\\JIT-PC1", "SUBDEV1\\user1")]
        [TestCase("OU=Dynamic JIT Groups,OU=LAPS Testing,DC=SUBDEV1,DC=IDMDEV1,DC=LOCAL", "SUBDEV1\\PC1$", "SUBDEV1\\JIT-PC1", "SUBDEV1\\user1")]
        [TestCase("OU=Dynamic JIT Groups,OU=LAPS Testing,DC=SUBDEV1,DC=IDMDEV1,DC=LOCAL", "SUBDEV1\\PC1$", "SUBDEV1\\JIT-PC1", "IDMDEV1\\user1")]
        [TestCase("OU=Dynamic JIT Groups,OU=LAPS Testing,DC=EXTDEV1,DC=LOCAL", "EXTDEV1\\PC1$", "EXTDEV1\\JIT-PC1", "EXTDEV1\\user1")]
        [TestCase("OU=Dynamic JIT Groups,OU=LAPS Testing,DC=EXTDEV1,DC=LOCAL", "EXTDEV1\\PC1$", "EXTDEV1\\JIT-PC1", "IDMDEV1\\user1")]
        [TestCase("OU=Dynamic JIT Groups,OU=LAPS Testing,DC=EXTDEV1,DC=LOCAL", "EXTDEV1\\PC1$", "EXTDEV1\\JIT-PC1", "SUBDEV1\\user1")]
        public void CreateDynamicGroup(string groupou, string computerName, string jitGroupName, string userName)
        {
            string groupname = Guid.NewGuid().ToString();
            string fqGroupName = $"{jitGroupName.Split('\\')[0]}\\{groupname}";

            IGroup jitGroup = directory.GetGroup(jitGroupName);
            jitGroup.RemoveMembers();
            IUser user = directory.GetUser(userName);
            IComputer computer = directory.GetComputer(computerName);

            options.DynamicGroupMappings.Add(new JitDynamicGroupMapping()
            {
                Domain = jitGroup.Sid.AccountDomainSid.ToString(),
                GroupOU = groupou,
                GroupNameTemplate = groupname
            });

            this.provider = new JitAccessProvider(directory, logger, this.GetOptions(), discoveryServices);

            provider.GrantJitAccessDynamicGroup(jitGroup, user, computer, false, TimeSpan.FromMinutes(1), out _);
            Thread.Sleep(TimeSpan.FromSeconds(20));

            IGroup ttlGroup = directory.GetGroup(fqGroupName);
            Assert.IsNotNull(ttlGroup);
            Assert.AreEqual(groupname, ttlGroup.SamAccountName);

            directory.IsSidInPrincipalToken(ttlGroup.Sid, jitGroup.Sid);
            directory.IsSidInPrincipalToken(user.Sid, ttlGroup.Sid);
            directory.IsSidInPrincipalToken(user.Sid, jitGroup.Sid);
        }

        [TestCase("OU=Dynamic JIT Groups,OU=LAPS Testing,DC=IDMDEV1,DC=LOCAL", "IDMDEV1\\PC1$", "IDMDEV1\\JIT-PC1", "IDMDEV1\\user1")]
        [TestCase("OU=Dynamic JIT Groups,OU=LAPS Testing,DC=EXTDEV1,DC=LOCAL", "EXTDEV1\\PC1$", "EXTDEV1\\JIT-PC1", "EXTDEV1\\user1")]
        [TestCase("OU=Dynamic JIT Groups,OU=LAPS Testing,DC=SUBDEV1,DC=IDMDEV1,DC=LOCAL", "SUBDEV1\\PC1$", "SUBDEV1\\JIT-PC1", "SUBDEV1\\user1")]
        public void TestDynamicGroupAccessExtensionNotAllowed(string groupou, string computerName, string jitGroupName, string userName)
        {
            string groupname = Guid.NewGuid().ToString();
            string fqGroupName = $"{jitGroupName.Split('\\')[0]}\\{groupname}";

            IGroup jitGroup = directory.GetGroup(jitGroupName);
            jitGroup.RemoveMembers();
            IUser user = directory.GetUser(userName);
            IComputer computer = directory.GetComputer(computerName);

            options.DynamicGroupMappings.Add(new JitDynamicGroupMapping()
            {
                Domain = jitGroup.Sid.AccountDomainSid.ToString(),
                GroupOU = groupou,
                GroupNameTemplate = groupname
            });

            this.provider = new JitAccessProvider(directory, logger, this.GetOptions(), discoveryServices);

            TimeSpan allowedAccess = provider.GrantJitAccessDynamicGroup(jitGroup, user, computer, false, TimeSpan.FromMinutes(1), out _);

            Assert.AreEqual(1, allowedAccess.TotalMinutes);

            Thread.Sleep(TimeSpan.FromSeconds(20));

            TimeSpan allowedAccess2 = provider.GrantJitAccessDynamicGroup(jitGroup, user, computer, false, TimeSpan.FromMinutes(1), out _);

            Assert.LessOrEqual(allowedAccess2.TotalSeconds, 50);
        }

        [TestCase("OU=Dynamic JIT Groups,OU=LAPS Testing,DC=IDMDEV1,DC=LOCAL", "IDMDEV1\\PC1$", "IDMDEV1\\JIT-PC1", "IDMDEV1\\user1")]
        [TestCase("OU=Dynamic JIT Groups,OU=LAPS Testing,DC=EXTDEV1,DC=LOCAL", "EXTDEV1\\PC1$", "EXTDEV1\\JIT-PC1", "EXTDEV1\\user1")]
        [TestCase("OU=Dynamic JIT Groups,OU=LAPS Testing,DC=SUBDEV1,DC=IDMDEV1,DC=LOCAL", "SUBDEV1\\PC1$", "SUBDEV1\\JIT-PC1", "SUBDEV1\\user1")]
        public void TestDynamicGroupAccessExtensionAllowed(string groupou, string computerName, string jitGroupName, string userName)
        {
            string groupname = Guid.NewGuid().ToString();
            string fqGroupName = $"{jitGroupName.Split('\\')[0]}\\{groupname}";

            IGroup jitGroup = directory.GetGroup(jitGroupName);
            jitGroup.RemoveMembers();
            IUser user = directory.GetUser(userName);
            IComputer computer = directory.GetComputer(computerName);

            options.DynamicGroupMappings.Add(new JitDynamicGroupMapping()
            {
                Domain = jitGroup.Sid.AccountDomainSid.ToString(),
                GroupOU = groupou,
                GroupNameTemplate = groupname
            });

            this.provider = new JitAccessProvider(directory, logger, this.GetOptions(), discoveryServices);

            TimeSpan allowedAccess = provider.GrantJitAccessDynamicGroup(jitGroup, user, computer, true, TimeSpan.FromMinutes(1), out _);

            Assert.AreEqual(1, allowedAccess.TotalMinutes);

            Thread.Sleep(TimeSpan.FromSeconds(20));

            TimeSpan allowedAccess2 = provider.GrantJitAccessDynamicGroup(jitGroup, user, computer, true, TimeSpan.FromMinutes(1), out _);

            Assert.AreEqual(allowedAccess2.TotalSeconds, 60);
        }

        [TestCase("OU=Dynamic JIT Groups,OU=LAPS Testing,DC=IDMDEV1,DC=LOCAL", "IDMDEV1\\PC1$", "IDMDEV1\\JIT-PC1", "IDMDEV1\\user1")]
        [TestCase("OU=Dynamic JIT Groups,OU=LAPS Testing,DC=IDMDEV1,DC=LOCAL", "IDMDEV1\\PC1$", "IDMDEV1\\JIT-PC1", "SUBDEV1\\user1")]
        [TestCase("OU=Dynamic JIT Groups,OU=LAPS Testing,DC=SUBDEV1,DC=IDMDEV1,DC=LOCAL", "SUBDEV1\\PC1$", "SUBDEV1\\JIT-PC1", "SUBDEV1\\user1")]
        [TestCase("OU=Dynamic JIT Groups,OU=LAPS Testing,DC=SUBDEV1,DC=IDMDEV1,DC=LOCAL", "SUBDEV1\\PC1$", "SUBDEV1\\JIT-PC1", "IDMDEV1\\user1")]
        [TestCase("OU=Dynamic JIT Groups,OU=LAPS Testing,DC=EXTDEV1,DC=LOCAL", "EXTDEV1\\PC1$", "EXTDEV1\\JIT-PC1", "EXTDEV1\\user1")]
        [TestCase("OU=Dynamic JIT Groups,OU=LAPS Testing,DC=EXTDEV1,DC=LOCAL", "EXTDEV1\\PC1$", "EXTDEV1\\JIT-PC1", "IDMDEV1\\user1")]
        [TestCase("OU=Dynamic JIT Groups,OU=LAPS Testing,DC=EXTDEV1,DC=LOCAL", "EXTDEV1\\PC1$", "EXTDEV1\\JIT-PC1", "SUBDEV1\\user1")]
        public void TestDynamicGroupAccessUndo(string groupou, string computerName, string jitGroupName, string userName)
        {
            string groupname = Guid.NewGuid().ToString();
            string fqGroupName = $"{jitGroupName.Split('\\')[0]}\\{groupname}";

            IGroup jitGroup = directory.GetGroup(jitGroupName);
            jitGroup.RemoveMembers();
            IUser user = directory.GetUser(userName);
            IComputer computer = directory.GetComputer(computerName);

            options.DynamicGroupMappings.Add(new JitDynamicGroupMapping()
            {
                Domain = jitGroup.Sid.AccountDomainSid.ToString(),
                GroupOU = groupou,
                GroupNameTemplate = groupname
            });

            this.provider = new JitAccessProvider(directory, logger, this.GetOptions(), discoveryServices);

            provider.GrantJitAccessDynamicGroup(jitGroup, user, computer, true, TimeSpan.FromMinutes(1), out Action undo);

            Thread.Sleep(TimeSpan.FromSeconds(20));

            Assert.IsNotNull(this.directory.GetGroup(fqGroupName));
            undo();

            Thread.Sleep(TimeSpan.FromSeconds(20));
            Assert.IsFalse(this.directory.TryGetGroup(fqGroupName, out _));
        }


        [TestCase("IDMDEV1\\JIT-PC1", "IDMDEV1\\PC1$", "IDMDEV1\\user1")]
        [TestCase("SUBDEV1\\JIT-PC1", "SUBDEV1\\PC1$", "SUBDEV1\\user1")]
        [TestCase("IDMDEV1\\JIT-PC1", "IDMDEV1\\PC1$", "SUBDEV1\\user1")]
        [TestCase("SUBDEV1\\JIT-PC1", "SUBDEV1\\PC1$", "IDMDEV1\\user1")]
        [TestCase("EXTDEV1\\JIT-PC1", "EXTDEV1\\PC1$", "SUBDEV1\\user1")]
        [TestCase("EXTDEV1\\JIT-PC1", "EXTDEV1\\PC1$", "EXTDEV1\\user1")]
        [TestCase("EXTDEV1\\JIT-PC1", "EXTDEV1\\PC1$", "IDMDEV1\\user1")]
        public void TestPamGroupAccessExtensionAllowed(string jitGroupName, string computerName, string userName)
        {
            IGroup jitGroup = directory.GetGroup(jitGroupName);
            jitGroup.RemoveMembers();
            IUser user = directory.GetUser(userName);
            IComputer computer = directory.GetComputer(computerName);

            this.provider = new JitAccessProvider(directory, logger, this.GetOptions(), discoveryServices);

            TimeSpan allowedAccess = provider.GrantJitAccessPam(jitGroup, user, computer, true, TimeSpan.FromMinutes(1), out _);
            TimeSpan? actualTtl = jitGroup.GetMemberTtl(user);

            Assert.AreEqual(1, allowedAccess.TotalMinutes);
            Assert.IsNotNull(actualTtl);
            Assert.LessOrEqual(actualTtl.Value.TotalSeconds, 60);

            Thread.Sleep(TimeSpan.FromSeconds(10));

            TimeSpan allowedAccess2 = provider.GrantJitAccessPam(jitGroup, user, computer, true, TimeSpan.FromMinutes(2), out _);

            actualTtl = jitGroup.GetMemberTtl(user);

            Assert.AreEqual(2, allowedAccess2.TotalMinutes);
            Assert.IsNotNull(actualTtl);
            Assert.LessOrEqual(actualTtl.Value.TotalSeconds, 3600);
        }


        [TestCase("IDMDEV1\\JIT-PC1", "IDMDEV1\\PC1$", "IDMDEV1\\user1")]
        [TestCase("SUBDEV1\\JIT-PC1", "SUBDEV1\\PC1$", "SUBDEV1\\user1")]
        [TestCase("IDMDEV1\\JIT-PC1", "IDMDEV1\\PC1$", "SUBDEV1\\user1")]
        [TestCase("SUBDEV1\\JIT-PC1", "SUBDEV1\\PC1$", "IDMDEV1\\user1")]
        [TestCase("EXTDEV1\\JIT-PC1", "EXTDEV1\\PC1$", "SUBDEV1\\user1")]
        [TestCase("EXTDEV1\\JIT-PC1", "EXTDEV1\\PC1$", "EXTDEV1\\user1")]
        [TestCase("EXTDEV1\\JIT-PC1", "EXTDEV1\\PC1$", "IDMDEV1\\user1")]
        public void TestPamGroupAccessExtensionNotAllowed(string jitGroupName, string computerName, string userName)
        {
            IGroup jitGroup = directory.GetGroup(jitGroupName);
            jitGroup.RemoveMembers();
            IUser user = directory.GetUser(userName);
            IComputer computer = directory.GetComputer(computerName);

            this.provider = new JitAccessProvider(directory, logger, this.GetOptions(), discoveryServices);

            TimeSpan allowedAccess = provider.GrantJitAccessPam(jitGroup, user, computer, false, TimeSpan.FromMinutes(1), out _);
            TimeSpan? actualTtl = jitGroup.GetMemberTtl(user);

            Assert.AreEqual(1, allowedAccess.TotalMinutes);
            Assert.IsNotNull(actualTtl);
            Assert.LessOrEqual(actualTtl.Value.TotalSeconds, 60);

            Thread.Sleep(TimeSpan.FromSeconds(10));

            TimeSpan allowedAccess2 = provider.GrantJitAccessPam(jitGroup, user, computer, false, TimeSpan.FromMinutes(2), out _);

            actualTtl = jitGroup.GetMemberTtl(user);

            Assert.IsNotNull(actualTtl);
            Assert.LessOrEqual(actualTtl.Value.TotalSeconds, 60);
            Assert.LessOrEqual(allowedAccess2.TotalSeconds, 60);
        }

        [TestCase("IDMDEV1\\JIT-PC1", "IDMDEV1\\PC1$", "IDMDEV1\\user1")]
        [TestCase("SUBDEV1\\JIT-PC1", "SUBDEV1\\PC1$", "SUBDEV1\\user1")]
        [TestCase("IDMDEV1\\JIT-PC1", "IDMDEV1\\PC1$", "SUBDEV1\\user1")]
        [TestCase("SUBDEV1\\JIT-PC1", "SUBDEV1\\PC1$", "IDMDEV1\\user1")]
        [TestCase("EXTDEV1\\JIT-PC1", "EXTDEV1\\PC1$", "SUBDEV1\\user1")]
        [TestCase("EXTDEV1\\JIT-PC1", "EXTDEV1\\PC1$", "EXTDEV1\\user1")]
        [TestCase("EXTDEV1\\JIT-PC1", "EXTDEV1\\PC1$", "IDMDEV1\\user1")]
        public void TestPamGroupAccessUndo(string jitGroupName, string computerName, string userName)
        {
            IGroup jitGroup = directory.GetGroup(jitGroupName);
            jitGroup.RemoveMembers();
            IUser user = directory.GetUser(userName);
            IComputer computer = directory.GetComputer(computerName);

            this.provider = new JitAccessProvider(directory, logger, this.GetOptions(), discoveryServices);

            TimeSpan allowedAccess = provider.GrantJitAccessPam(jitGroup, user, computer, false, TimeSpan.FromMinutes(1), out Action undo);
            TimeSpan? actualTtl = jitGroup.GetMemberTtl(user);

            Assert.AreEqual(1, allowedAccess.TotalMinutes);
            Assert.IsNotNull(actualTtl);
            Assert.LessOrEqual(actualTtl.Value.TotalSeconds, 60);

            undo();
            Assert.IsNull(jitGroup.GetMemberTtl(user));
        }


        [TestCase("IDMDEV1\\JIT-PC1", "IDMDEV1\\PC1$", "IDMDEV1\\user1")]
        public void ThrowOnNoMappingForDomain(string jitGroupName, string computerName, string userName)
        {
            IGroup jitGroup = directory.GetGroup(jitGroupName);
            jitGroup.RemoveMembers();
            IUser user = directory.GetUser(userName);
            IComputer computer = directory.GetComputer(computerName);

            this.provider = new JitAccessProvider(directory, logger, this.GetOptions(), discoveryServices);

            Assert.Throws<NoDynamicGroupMappingForDomainException>(() => provider.GrantJitAccessDynamicGroup(jitGroup, user, computer, false, TimeSpan.FromMinutes(1), out _));
        }

        [TestCase("IDMDEV1\\JIT-PC1", "IDMDEV1\\PC1$", "IDMDEV1\\user1")]
        [TestCase("SUBDEV1\\JIT-PC1", "SUBDEV1\\PC1$", "SUBDEV1\\user1")]
        [TestCase("IDMDEV1\\JIT-PC1", "IDMDEV1\\PC1$", "SUBDEV1\\user1")]
        [TestCase("SUBDEV1\\JIT-PC1", "SUBDEV1\\PC1$", "IDMDEV1\\user1")]
        [TestCase("EXTDEV1\\JIT-PC1", "EXTDEV1\\PC1$", "SUBDEV1\\user1")]
        [TestCase("EXTDEV1\\JIT-PC1", "EXTDEV1\\PC1$", "EXTDEV1\\user1")]
        [TestCase("EXTDEV1\\JIT-PC1", "EXTDEV1\\PC1$", "IDMDEV1\\user1")]
        public void AddUserToGroupPam(string jitGroupName, string computerName, string userName)
        {
            IGroup jitGroup = directory.GetGroup(jitGroupName);
            jitGroup.RemoveMembers();
            IUser user = directory.GetUser(userName);
            IComputer computer = directory.GetComputer(computerName);

            this.provider = new JitAccessProvider(directory, logger, this.GetOptions(), discoveryServices);

            provider.GrantJitAccessPam(jitGroup, user, computer, false, TimeSpan.FromMinutes(1), out _);

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