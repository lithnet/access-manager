using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Security.Principal;
using System.Text;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Lithnet.AccessManager.Server.Test
{
    public class JitAccessGroupResolverTests
    {
        private IDirectory directory;

        private JitAccessGroupResolver resolver;

        [SetUp()]
        public void TestInitialize()
        {
            directory = new ActiveDirectory();
            this.resolver = new JitAccessGroupResolver(directory);
        }

        [TestCase("IDMDEV1\\JIT-PC1", "IDMDEV1\\PC1")]
        [TestCase("SUBDEV1\\JIT-PC1", "SUBDEV1\\PC1")]
        [TestCase("EXTDEV1\\JIT-PC1", "EXTDEV1\\PC1")]
        public void GetGroupByTemplate(string groupName, string computerName)
        {
            IComputer computer = directory.GetComputer(computerName);
            IGroup group = resolver.GetJitGroup(computer, "{computerDomain}\\JIT-{computerName}");
            Assert.AreEqual(groupName, group.MsDsPrincipalName);
        }

        [TestCase("IDMDEV1\\JIT-PC1", "IDMDEV1\\PC1")]
        [TestCase("SUBDEV1\\JIT-PC1", "SUBDEV1\\PC1")]
        [TestCase("EXTDEV1\\JIT-PC1", "EXTDEV1\\PC1")]
        public void GetGroupByName(string groupName, string computerName)
        {
            IComputer computer = directory.GetComputer(computerName);
            IGroup group = resolver.GetJitGroup(computer, groupName);

            Assert.AreEqual(groupName, group.MsDsPrincipalName);
        }

        [TestCase("IDMDEV1\\JIT-PC1", "IDMDEV1\\PC1")]
        [TestCase("SUBDEV1\\JIT-PC1", "SUBDEV1\\PC1")]
        [TestCase("EXTDEV1\\JIT-PC1", "EXTDEV1\\PC1")]
        public void GetGroupBySid(string groupName, string computerName)
        {
            IComputer computer = directory.GetComputer(computerName);
            IGroup sourceGroup = directory.GetGroup(groupName);
            IGroup group = resolver.GetJitGroup(computer, sourceGroup.Sid.ToString());
            
            Assert.AreEqual(groupName, group.MsDsPrincipalName);
        }
    }
}