using System;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

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

        [TestCase("PC1", "IDMDEV1", "{computerDomain}\\{computerName}", "IDMDEV1\\PC1")]
        [TestCase("PC1", "IDMDEV1", "IDMDEV2\\{computerName}", "IDMDEV2\\PC1")]
        [TestCase("PC1", "IDMDEV1", "IDMDEV2\\PC3", "IDMDEV2\\PC3")]
        [TestCase("PC1", "IDMDEV1", "Something", "IDMDEV1\\Something")]
        public void TestBuildNameFromTemplate(string computerName, string domain, string template, string expected)
        {
            string actual = this.resolver.BuildGroupName(template, domain, computerName);
            Assert.AreEqual(expected, actual);
        }

        [TestCase("IDMDEV1\\PC1", "IDMDEV1\\JIT-{ComputerName}", "IDMDEV1\\JIT-PC1")]
        [TestCase("IDMDEV1\\PC1", "IDMDEV1\\JIT-PC2", "IDMDEV1\\JIT-PC2")]
        [TestCase("IDMDEV1\\PC1", "{computerDomain}\\JIT-{computerName}", "IDMDEV1\\JIT-PC1")]
        public void GetGroupFromName(string computerName, string template, string expected)
        {
            IComputer computer = this.directory.GetComputer(computerName);
            IGroup group = this.resolver.GetJitGroup(computer, template);

            Assert.AreEqual(expected, group.MsDsPrincipalName.ToUpper());
        }

        [TestCase("IDMDEV1\\JIT-PC1")]
        public void GetGroupFromSid(string groupName)
        {
            IGroup group = this.directory.GetGroup(groupName);
            IComputer computer = this.directory.GetComputer("IDMDEV1\\PC1");

            IGroup found = this.resolver.GetJitGroup(computer, group.Sid.ToString());

            Assert.AreEqual(group.Sid, found.Sid);
        }
    }
}
