using Moq;
using NUnit.Framework;
using System.Security.Principal;
using Lithnet.AccessManager.Agent.Providers;

namespace Lithnet.AccessManager.Agent.Windows.Test
{
    [TestFixture()]
    public class WindowsPasswordChangeProviderTests
    {
        private WindowsPasswordChangeProvider provider;
        private Mock<ILocalSam> localSamMock;

        [SetUp()]
        public void TestInitialize()
        {
            this.localSamMock = new Mock<ILocalSam>();
            this.localSamMock.Setup(t => t.SetLocalAccountPassword(It.IsAny<SecurityIdentifier>(), It.IsAny<string>()));
            this.localSamMock.Setup(t => t.EnableLocalAccount(It.IsAny<SecurityIdentifier>()));
            this.localSamMock.Setup(t => t.GetWellKnownSid(It.Is<WellKnownSidType>(u => u == WellKnownSidType.BuiltinAdministratorsSid))).Returns(new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null));
            this.localSamMock.Setup(t => t.GetBuiltInAdministratorAccountName()).Returns("Administrator2");
            var localSam = this.localSamMock.Object;

            this.provider = new WindowsPasswordChangeProvider(localSam);
        }

        [Test()]
        public void GetAccountNameTest()
        {
            StringAssert.AreEqualIgnoringCase("Administrator2", this.provider.GetAccountName());
        }

        [Test()]
        public void ChangePasswordTest()
        {
            string password = "mypassword";
            this.provider.ChangePassword(password);
            this.localSamMock.Verify(v => v.SetLocalAccountPassword(It.IsAny<SecurityIdentifier>(), It.Is<string>(u => u == password)));
        }

        [Test()]
        public void EnsureEnabledTest()
        {
            this.provider.EnsureEnabled();
            this.localSamMock.Verify(v => v.EnableLocalAccount(It.IsAny<SecurityIdentifier>()));
        }
    }
}