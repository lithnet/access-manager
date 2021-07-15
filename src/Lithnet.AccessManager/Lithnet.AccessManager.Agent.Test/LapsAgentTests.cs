using System;
using System.Security.Principal;
using Lithnet.AccessManager.Agent.Providers;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Lithnet.AccessManager.Agent.Test
{
    public class LapsAgentTests
    {
        private Mock<IActiveDirectory> directory;
        private Mock<IActiveDirectoryLapsSettingsProvider> settings;
        private Mock<IPasswordGenerator> passwordGenerator;
        private Mock<ILocalSam> sam;
        private Mock<ILithnetAdminPasswordProvider> lithnetPwdProvider;
        private Mock<IActiveDirectoryComputer> computer;

        [SetUp()]
        public void TestInitialize()
        {
            this.directory = new Mock<IActiveDirectory>();
            this.settings = new Mock<IActiveDirectoryLapsSettingsProvider>();
            this.passwordGenerator = new Mock<IPasswordGenerator>();
            this.sam = new Mock<ILocalSam>();
            this.lithnetPwdProvider = new Mock<ILithnetAdminPasswordProvider>();
            this.computer = new Mock<IActiveDirectoryComputer>();
        }

        [Test]
        public void TestPasswordChangeAppData()
        {
            ActiveDirectoryLapsAgent agent = this.BuildAgent();

            agent.ChangePassword(this.computer.Object);

            lithnetPwdProvider.Verify(v => v.UpdateCurrentPassword(It.IsAny<IActiveDirectoryComputer>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int>(), PasswordAttributeBehaviour.Ignore), Times.Once);
            sam.Verify(v => v.SetLocalAccountPassword(It.IsAny<SecurityIdentifier>(), It.IsAny<string>()));
        }

        [Test]
        public void HasPasswordExpiredAppDataNull()
        {
            lithnetPwdProvider.Setup(a => a.GetExpiry(It.IsAny<IActiveDirectoryComputer>())).Returns((DateTime?)null);
            ActiveDirectoryLapsAgent agent = this.BuildAgent();

            Assert.IsFalse(agent.HasPasswordExpired(this.computer.Object));
        }

        [Test]
        public void HasPasswordExpiredAppDataExpired()
        {
            lithnetPwdProvider.Setup(a => a.HasPasswordExpired(It.IsAny<IActiveDirectoryComputer>(), false)).Returns(true);

            ActiveDirectoryLapsAgent agent = this.BuildAgent();

            Assert.IsTrue(agent.HasPasswordExpired(this.computer.Object));
        }

        [Test]
        public void HasPasswordExpiredAppDataNotExpired()
        {
            lithnetPwdProvider.Setup(a => a.GetExpiry(It.IsAny<IActiveDirectoryComputer>())).Returns(DateTime.UtcNow.AddDays(1));

            ActiveDirectoryLapsAgent agent = this.BuildAgent();

            Assert.IsFalse(agent.HasPasswordExpired(this.computer.Object));
        }

        private ActiveDirectoryLapsAgent BuildAgent(IActiveDirectoryLapsSettingsProvider settings = null, IActiveDirectory directory = null, IPasswordGenerator passwordGenerator = null, ILocalSam sam = null, ILithnetAdminPasswordProvider lithnetProvider = null)
        {
            return new ActiveDirectoryLapsAgent(
                Mock.Of<ILogger<ActiveDirectoryLapsAgent>>(),
                directory ?? this.directory.Object,
                settings ?? this.settings.Object,
                passwordGenerator ?? this.passwordGenerator.Object,
                sam ?? this.sam.Object,
                lithnetProvider ?? this.lithnetPwdProvider.Object);
        }
    }
}