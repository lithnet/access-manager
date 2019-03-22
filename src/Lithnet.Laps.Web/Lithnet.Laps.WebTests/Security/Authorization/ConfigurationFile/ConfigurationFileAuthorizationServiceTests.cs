using NUnit.Framework;
using System;
using Lithnet.Laps.Web.Models;
using Moq;
using NLog;

namespace Lithnet.Laps.Web.Security.Authorization.ConfigurationFile.Tests
{
    [TestFixture()]
    public class ConfigurationFileAuthorizationServiceTests
    {
        private Mock<ILogger> dummyLogger;
        private Mock<ITarget> targetStub;
        private Mock<IComputer> dummyComputer;

        [SetUp()]
        public void TestInitialize()
        {
            dummyLogger = new Mock<ILogger>();
            targetStub = new Mock<ITarget>();
            dummyComputer = new Mock<IComputer>();

            // When ExpireAfter is left out in the configuration file, I have the impression
            // that this is passed as an empty string. So let's emulate this.
            targetStub
                .Setup(t => t.ExpireAfter)
                .Returns(String.Empty);
        }

        [Test()]
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
                .Setup(ar => ar.GetReadersForTarget(targetStub.Object))
                .Returns(new[] { readerStub.Object }).Verifiable();

            var service = new ConfigurationFileAuthorizationService(
                dummyLogger.Object,
                directoryStub.Object,
                availableReadersStub.Object
            );

            var authorizationResponse =
                service.CanAccessPassword(userStub.Object, dummyComputer.Object, targetStub.Object);

            availableReadersStub.Verify();

            Assert.IsTrue(authorizationResponse.IsAuthorized);
            // The reader principal should be in extra info.
            Assert.AreEqual("DOMAIN\\username", authorizationResponse.ExtraInfo);
        }
    }
}