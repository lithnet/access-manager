using System;
using System.Security.Principal;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Lithnet.AccessManager.Agent.Test
{
    public class LapsAgentTests
    {
        private Mock<IDirectory> directory;

        private Mock<ILapsSettings> settings;

        private Mock<IPasswordGenerator> passwordGenerator;

        private Mock<ILocalSam> sam;
        
        private Mock<ILithnetAdminPasswordProvider> lithnetPwdProvider;

        private Mock<IComputer> computer;

        [SetUp()]
        public void TestInitialize()
        {
            this.directory = new Mock<IDirectory>();
            this.settings = new Mock<ILapsSettings>();
            this.passwordGenerator = new Mock<IPasswordGenerator>();
            this.sam = new Mock<ILocalSam>();
            this.lithnetPwdProvider = new Mock<ILithnetAdminPasswordProvider>();
            this.computer = new Mock<IComputer>();
        }

        [Test]
        public void TestExitOnAgentDisabled()
        {
            this.settings.SetupGet(a => a.Enabled).Returns(false);

            LapsAgent agent = this.BuildAgent();

            agent.DoCheck();
            settings.VerifyGet(t => t.Enabled);
            settings.VerifyGet(t => t.MsMcsAdmPwdBehaviour, Times.Never);
        }

        [Test]
        public void TestPasswordChangeAppData()
        {
            LapsAgent agent = this.BuildAgent();

            agent.ChangePassword(this.computer.Object);

            lithnetPwdProvider.Verify(v => v.UpdateCurrentPassword(It.IsAny<IComputer>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int>(), MsMcsAdmPwdBehaviour.Ignore), Times.Once);
            sam.Verify(v => v.SetLocalAccountPassword(It.IsAny<SecurityIdentifier>(), It.IsAny<string>()));
        }

        [Test]
        public void HasPasswordExpiredAppDataNull()
        {
            lithnetPwdProvider.Setup(a => a.GetExpiry(It.IsAny<IComputer>())).Returns((DateTime?)null);
            LapsAgent agent = this.BuildAgent();

            Assert.IsFalse(agent.HasPasswordExpired(this.computer.Object));
        }

        [Test]
        public void HasPasswordExpiredAppDataExpired()
        {
            lithnetPwdProvider.Setup(a => a.HasPasswordExpired(It.IsAny<IComputer>(), false)).Returns(true);

            LapsAgent agent = this.BuildAgent();

            Assert.IsTrue(agent.HasPasswordExpired(this.computer.Object));
        }

        [Test]
        public void HasPasswordExpiredAppDataNotExpired()
        {
            lithnetPwdProvider.Setup(a => a.GetExpiry(It.IsAny<IComputer>())).Returns(DateTime.UtcNow.AddDays(1));

            LapsAgent agent = this.BuildAgent();

            Assert.IsFalse(agent.HasPasswordExpired(this.computer.Object));
        }

        private LapsAgent BuildAgent(ILapsSettings settings = null, IDirectory directory = null, IPasswordGenerator passwordGenerator = null, ILocalSam sam = null, ILithnetAdminPasswordProvider lithnetProvider = null)
        {
            return new LapsAgent(
                Mock.Of<ILogger<LapsAgent>>(),
                directory ?? this.directory.Object,
                settings ?? this.settings.Object,
                passwordGenerator ?? this.passwordGenerator.Object,
                sam ?? this.sam.Object,
                lithnetProvider ?? this.lithnetPwdProvider.Object);
        }
    }
}