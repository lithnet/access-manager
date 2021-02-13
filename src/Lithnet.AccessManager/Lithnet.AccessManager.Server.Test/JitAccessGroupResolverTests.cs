using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Security.Principal;
using System.Text;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using C = Lithnet.AccessManager.Test.TestEnvironmentConstants;

namespace Lithnet.AccessManager.Server.Test
{
    public class JitAccessGroupResolverTests
    {
        private IDirectory directory;

        private JitAccessGroupResolver resolver;

        private IDiscoveryServices discoveryServices;

        [SetUp()]
        public void TestInitialize()
        {
            this.discoveryServices = new DiscoveryServices(Mock.Of<ILogger<DiscoveryServices>>());
            directory = new ActiveDirectory(discoveryServices);
            this.resolver = new JitAccessGroupResolver(directory, discoveryServices);
        }

        [TestCase(C.Dev + "\\JIT-PC1", C.DEV_PC1)]
        [TestCase(C.SubDev + "\\JIT-PC1", C.SUBDEV_PC1)]
        [TestCase(C.ExtDev + "\\JIT-PC1", C.EXTDEV_PC1)]
        public void GetGroupByTemplate(string groupName, string computerName)
        {
            IComputer computer = directory.GetComputer(computerName);
            IGroup group = resolver.GetJitGroup(computer, "%computerDomain%\\JIT-%computerName%");
            Assert.AreEqual(groupName, group.MsDsPrincipalName);
        }

        [TestCase(C.Dev + "\\JIT-PC1", C.DEV_PC1)]
        [TestCase(C.SubDev + "\\JIT-PC1", C.SUBDEV_PC1)]
        [TestCase(C.ExtDev + "\\JIT-PC1", C.EXTDEV_PC1)]
        public void GetGroupByName(string groupName, string computerName)
        {
            IComputer computer = directory.GetComputer(computerName);
            IGroup group = resolver.GetJitGroup(computer, groupName);

            Assert.AreEqual(groupName, group.MsDsPrincipalName);
        }

        [TestCase(C.Dev + "\\JIT-PC1", C.DEV_PC1)]
        [TestCase(C.SubDev + "\\JIT-PC1", C.SUBDEV_PC1)]
        [TestCase(C.ExtDev + "\\JIT-PC1", C.EXTDEV_PC1)]
        public void GetGroupBySid(string groupName, string computerName)
        {
            IComputer computer = directory.GetComputer(computerName);
            IGroup sourceGroup = directory.GetGroup(groupName);
            IGroup group = resolver.GetJitGroup(computer, sourceGroup.Sid.ToString());
            
            Assert.AreEqual(groupName, group.MsDsPrincipalName);
        }
    }
}