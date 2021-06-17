using System;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using C = Lithnet.AccessManager.Test.TestEnvironmentConstants;

namespace Lithnet.AccessManager.Test
{
    public class JitAccessGroupResolverTests
    {
        private Mock<IAppPathProvider> env;

        private IDirectory directory;

        private IDiscoveryServices discoveryServices;

        private JitAccessGroupResolver resolver;

        [SetUp()]
        public void TestInitialize()
        {
            this.env = new Mock<IAppPathProvider>();
            this.env.SetupGet(t => t.AppPath).Returns(Environment.CurrentDirectory);
            this.discoveryServices = new DiscoveryServices(Mock.Of<ILogger<DiscoveryServices>>());
            this.directory = new ActiveDirectory(discoveryServices);
            this.resolver = new JitAccessGroupResolver(directory, discoveryServices);
        }

        [TestCase(C.PC1, C.Dev, "%computerDomain%\\%computerName%", C.DEV_PC1)]
        [TestCase(C.PC1, C.Dev, "IDMDEV2\\%computerName%", "IDMDEV2\\PC1")]
        [TestCase(C.PC1, C.Dev, "IDMDEV2\\PC3", "IDMDEV2\\PC3")]
        [TestCase(C.PC1, C.Dev, "Something", C.Dev + "\\Something")]
        public void TestBuildNameFromTemplate(string computerName, string domain, string template, string expected)
        {
            string actual = this.resolver.BuildGroupName(template, domain, computerName);
            Assert.AreEqual(expected, actual);
        }

        [TestCase(C.DEV_PC1, C.Dev + "\\JIT-%ComputerName%", C.DEV_JIT_PC1)]
        [TestCase(C.DEV_PC2, C.DEV_JIT_PC2, C.DEV_JIT_PC2)]
        [TestCase(C.DEV_PC1, "%computerDomain%\\JIT-%computerName%", C.DEV_JIT_PC1)]
        public void GetGroupFromName(string computerName, string template, string expected)
        {
            IActiveDirectoryComputer computer = this.directory.GetComputer(computerName);
            IGroup group = this.resolver.GetJitGroup(computer, template);

            Assert.AreEqual(expected, group.MsDsPrincipalName.ToUpper());
        }

        [TestCase(C.DEV_JIT_PC1)]
        public void GetGroupFromSid(string groupName)
        {
            IGroup group = this.directory.GetGroup(groupName);
            IActiveDirectoryComputer computer = this.directory.GetComputer(C.DEV_PC1);

            IGroup found = this.resolver.GetJitGroup(computer, group.Sid.ToString());

            Assert.AreEqual(group.Sid, found.Sid);
        }
    }
}
