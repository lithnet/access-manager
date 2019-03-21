using Lithnet.Laps.Web;
using Lithnet.Laps.Web.Models;
using Lithnet.Laps.Web.Security.Authorization.ConfigurationFile;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NLog;

namespace Lithnet.Laps.WebTests.Security.Authorization
{
    [TestClass()]
    public class ConfigurationFileAuthorizationServiceTests
    {
        private Mock<ILogger> dummyLogger;
        private Mock<ITarget> dummyTarget;
        private Mock<IComputer> dummyComputer;

        [TestInitialize]
        public void TestInitialize()
        {
            dummyLogger = new Mock<ILogger>();
            dummyTarget = new Mock<ITarget>();
            dummyComputer = new Mock<IComputer>();
        }

        [TestMethod()]
        public void CanAccessPasswordForUserPrincipalTest()
        {
            // I admit this is a bad test. It depends too much on the actual implementation.
            // An integration test would be better, but then I would have to know a user
            // from the domain of the developer.

            var readerStub = new Mock<IReaderElement>();
            var userStub = new Mock<IUser>();
            var directoryStub = new Mock<IDirectory>();
            var availableReadersStub = new Mock<IAvailableReaders>();

            readerStub
                .Setup(r => r.Principal)
                .Returns("DOMAIN\\username");

            directoryStub
                .Setup(d => d.GetUser(It.IsAny<string>()))
                .Returns(userStub.Object);

            userStub
                .Setup(u => u.DistinguishedName)
                .Returns("some-distinguished-name");

            availableReadersStub
                .Setup(ar => ar.GetReadersForTarget(dummyTarget.Object))
                .Returns(new [] {readerStub.Object});

            var service = new ConfigurationFileAuthorizationService(
                dummyLogger.Object,
                directoryStub.Object,
                availableReadersStub.Object
            );

            Assert.IsTrue(
                service.CanAccessPassword(userStub.Object, dummyComputer.Object, dummyTarget.Object).IsAuthorized
            );
        }
    }
}