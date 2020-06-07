using Lithnet.Laps.Web.ActiveDirectory;
using Lithnet.Laps.Web.AppSettings;
using Moq;
using NLog;
using NUnit.Framework;

namespace Lithnet.Laps.Web.Test
{
    public class Tests
    {

        private Mock<ILogger> dummyLogger;
        private Mock<IComputer> dummyComputer;
        private Mock<IUser> dummyUser;
        private Mock<IAuthenticationProvider> authProvider;

        [SetUp()]
        public void TestInitialize()
        {
            dummyLogger = new Mock<ILogger>();
            dummyComputer = new Mock<IComputer>();
            dummyUser = new Mock<IUser>();

            ActiveDirectory.ActiveDirectory d = new ActiveDirectory.ActiveDirectory();

            authProvider = new Mock<IAuthenticationProvider>();

            authProvider.Setup(a => a.GetLoggedInUser()).Returns(() => d.GetUser("mgr-rnewing"));
        }

        [Test]
        public void Test1()
        {
            

            //authProvider.Verify(v => v.GetLoggedInUser().SamAccountName).Returns("mgr-rnewing");
        }
    }
}