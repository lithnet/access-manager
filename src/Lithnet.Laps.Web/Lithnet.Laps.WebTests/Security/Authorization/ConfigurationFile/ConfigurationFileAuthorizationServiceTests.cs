using NUnit.Framework;
using Moq;
using NLog;
using Lithnet.Laps.Web.ActiveDirectory;

namespace Lithnet.Laps.Web.Security.Authorization.ConfigurationFile.Tests
{
    [TestFixture()]
    public class ConfigurationFileAuthorizationServiceTests
    {
        private Mock<ILogger> dummyLogger;
        private Mock<IComputer> dummyComputer;

        [SetUp()]
        public void TestInitialize()
        {
            dummyLogger = new Mock<ILogger>();
            dummyComputer = new Mock<IComputer>();
        }

        [Test()]
        public void CanAccessPasswordForUserPrincipalTest()
        {
            // I admit this is a bad test. It depends too much on the actual implementation.
            // An integration test would be better, but then I would have to know a user
            // from the domain of the developer.

            var userStub = new Mock<IUser>();
            var directoryStub = new Mock<IDirectory>();

            directoryStub
                .Setup(d => d.GetUser(It.IsAny<string>()))
                .Returns(userStub.Object);

            userStub
                .Setup(u => u.DistinguishedName)
                .Returns("some-distinguished-name");

        }
    }
}