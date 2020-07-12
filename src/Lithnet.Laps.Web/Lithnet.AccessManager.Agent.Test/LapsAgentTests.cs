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

        private Mock<IMsMcsAdmPwdProvider> admPwdProvider;

        private Mock<ILocalSam> sam;

        private Mock<IEncryptionProvider> encryptionProvider;

        private Mock<ICertificateProvider> certificateResolver;

        private Mock<IAppDataProvider> appDataProvider;

        private Mock<IAppData> appData;

        private Mock<IComputer> computer;

        [SetUp()]
        public void TestInitialize()
        {
            this.directory = new Mock<IDirectory>();
            this.settings = new Mock<ILapsSettings>();
            this.passwordGenerator = new Mock<IPasswordGenerator>();
            this.admPwdProvider = new Mock<IMsMcsAdmPwdProvider>();
            this.sam = new Mock<ILocalSam>();
            this.encryptionProvider = new Mock<IEncryptionProvider>();
            this.certificateResolver = new Mock<ICertificateProvider>();
            this.appDataProvider = new Mock<IAppDataProvider>();
            this.appData = new Mock<IAppData>();
            this.computer = new Mock<IComputer>();
        }

        [Test]
        public void TestExitOnAgentDisabled()
        {
            this.settings.SetupGet(a => a.Enabled).Returns(false);

            LapsAgent agent = this.BuildAgent();

            agent.DoCheck();
            settings.VerifyGet(t => t.Enabled);
            appDataProvider.Verify(t => t.GetAppData(It.IsAny<IComputer>()), Times.Never);
        }


        [Test]
        public void TestExitOnNoPasswordProvidersEnabled()
        {
            this.settings.SetupGet(a => a.Enabled).Returns(true);
            this.settings.SetupGet(a => a.WriteToAppData).Returns(false);
            this.settings.SetupGet(a => a.WriteToMsMcsAdmPasswordAttributes).Returns(false);

            LapsAgent agent = this.BuildAgent();

            agent.DoCheck();
            settings.VerifyGet(t => t.Enabled);
            appDataProvider.Verify(t => t.GetAppData(It.IsAny<IComputer>()), Times.Never);
        }

        [Test]
        public void TestPasswordChangeLaps()
        {
            this.settings.SetupGet(a => a.WriteToMsMcsAdmPasswordAttributes).Returns(true);
            LapsAgent agent = this.BuildAgent(); 

            agent.ChangePassword(this.appData.Object, this.computer.Object);

            admPwdProvider.Verify(v => v.SetPassword(It.IsAny<IComputer>(), It.IsAny<string>(), It.IsAny<DateTime>()));
            appData.Verify(v => v.UpdateCurrentPassword(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int>()), Times.Never);
            sam.Verify(v => v.SetLocalAccountPassword(It.IsAny<SecurityIdentifier>(), It.IsAny<string>()));
        }

        [Test]
        public void TestPasswordChangeAppData()
        {
            settings.SetupGet(a => a.WriteToAppData).Returns(true);

            LapsAgent agent = this.BuildAgent();

            agent.ChangePassword(appData.Object, this.computer.Object);

            admPwdProvider.Verify(v => v.SetPassword(It.IsAny<IComputer>(), It.IsAny<string>(), It.IsAny<DateTime>()), Times.Never);
            appData.Verify(v => v.UpdateCurrentPassword(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int>()), Times.Once);
            sam.Verify(v => v.SetLocalAccountPassword(It.IsAny<SecurityIdentifier>(), It.IsAny<string>()));
        }

        [Test]
        public void HasPasswordExpiredAppDataNull()
        {
            settings.SetupGet(a => a.WriteToAppData).Returns(true);
            appData.SetupGet(a => a.PasswordExpiry).Returns((DateTime?)null);
            LapsAgent agent = this.BuildAgent();

            Assert.IsFalse(agent.HasPasswordExpired(appData.Object, this.computer.Object));
        }

        [Test]
        public void HasPasswordExpiredAppDataExpired()
        {
            settings.SetupGet(a => a.WriteToAppData).Returns(true);
            appData.SetupGet(a => a.PasswordExpiry).Returns(DateTime.UtcNow.AddDays(-1));

            LapsAgent agent = this.BuildAgent();

            Assert.IsTrue(agent.HasPasswordExpired(appData.Object, this.computer.Object));
        }

        [Test]
        public void HasPasswordExpiredAppDataNotExpired()
        {
            settings.SetupGet(a => a.WriteToAppData).Returns(true);
            appData.SetupGet(a => a.PasswordExpiry).Returns(DateTime.UtcNow.AddDays(1));

            LapsAgent agent = this.BuildAgent();

            Assert.IsFalse(agent.HasPasswordExpired(appData.Object, this.computer.Object));
        }

        [Test]
        public void HasPasswordExpiredMsMcsAdmPwdNull()
        {
            settings.SetupGet(a => a.WriteToAppData).Returns(false);
            settings.SetupGet(a => a.WriteToMsMcsAdmPasswordAttributes).Returns(true);
            admPwdProvider.Setup(a => a.GetExpiry(null)).Returns((DateTime?)null);

            LapsAgent agent = this.BuildAgent();

            Assert.IsFalse(agent.HasPasswordExpired(null, this.computer.Object));
        }


        [Test]
        public void HasPasswordExpiredMsMcsAdmPwdExpired()
        {
            settings.SetupGet(a => a.WriteToAppData).Returns(false);
            settings.SetupGet(a => a.WriteToMsMcsAdmPasswordAttributes).Returns(true);
            admPwdProvider.Setup(a => a.GetExpiry(null)).Returns(DateTime.UtcNow.AddDays(-1));

            LapsAgent agent = this.BuildAgent();

            Assert.IsFalse(agent.HasPasswordExpired(null, this.computer.Object));
        }

        [Test]
        public void HasPasswordExpiredMsMcsAdmPwdNotExpired()
        {
            settings.SetupGet(a => a.WriteToAppData).Returns(false);
            settings.SetupGet(a => a.WriteToMsMcsAdmPasswordAttributes).Returns(true);
            admPwdProvider.Setup(a => a.GetExpiry(null)).Returns(DateTime.UtcNow.AddDays(1));

            LapsAgent agent = this.BuildAgent();

            Assert.IsFalse(agent.HasPasswordExpired(null, this.computer.Object));
        }

        private LapsAgent BuildAgent(ILapsSettings settings = null, IDirectory directory = null, IPasswordGenerator passwordGenerator = null, IMsMcsAdmPwdProvider admPwdProvider = null, ILocalSam sam = null, IEncryptionProvider encryptionProvider = null, ICertificateProvider certificateProvider = null, IAppDataProvider appDataProvider = null)
        {
            return new LapsAgent(
                Mock.Of<ILogger<LapsAgent>>(),
                directory ?? this.directory.Object,
                settings ?? this.settings.Object,
                passwordGenerator ?? this.passwordGenerator.Object,
                encryptionProvider ?? this.encryptionProvider.Object,
                certificateProvider ?? this.certificateResolver.Object,
                sam ?? this.sam.Object,
                appDataProvider ?? this.appDataProvider.Object,
                admPwdProvider ?? this.admPwdProvider.Object);
        }
    }
}