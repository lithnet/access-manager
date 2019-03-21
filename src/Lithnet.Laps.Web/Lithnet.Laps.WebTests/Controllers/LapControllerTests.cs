using NUnit.Framework;
using System;
using Lithnet.Laps.Web.Audit;
using Lithnet.Laps.Web.Models;
using Lithnet.Laps.Web.Security.Authentication;
using Lithnet.Laps.Web.Security.Authorization;
using Lithnet.Laps.Web.Security.Authorization.ConfigurationFile;
using Moq;
using NLog;

namespace Lithnet.Laps.Web.Controllers.Tests
{
    [TestFixture()]
    public class LapControllerTests
    {
        private Mock<ILogger> dummyLogger;
        private Mock<IDirectory> dummyDirectory;
        private Mock<IRateLimiter> dummyRateLimiter;
        private Mock<ITarget> dummyTarget;
        private Mock<IUser> dummyUser;

        [SetUp()]
        public void TestInitialize()
        {
            dummyLogger = new Mock<ILogger>();
            dummyDirectory = new Mock<IDirectory>();
            dummyRateLimiter = new Mock<IRateLimiter>();
            dummyTarget = new Mock<ITarget>();
            dummyUser = new Mock<IUser>();
        }

        [Test()]
        public void GetPassesTargetToReportingWhenAuthorizationFails()
        {
            var authenticationServiceStub = new Mock<IAuthenticationService>();
            var authorizationServiceStub = new Mock<IAuthorizationService>();
            var availableTargetsStub = new Mock<IAvailableTargets>();

            var reportingMock = new Mock<IReporting>();

            authenticationServiceStub
                .Setup(svc => svc.GetLoggedInUser())
                .Returns(dummyUser.Object);

            authorizationServiceStub
                .Setup(svc => svc.CanAccessPassword(It.IsAny<IUser>(), It.IsAny<IComputer>(), It.IsAny<ITarget>()))
                .Returns(AuthorizationResponse.Unauthorized());

            availableTargetsStub
                .Setup(svc => svc.GetMatchingTargetOrNull(It.IsAny<IComputer>()))
                .Returns(dummyTarget.Object);

            // Hmmm... this controller has a lot of dependencies.
            var controller = new LapController(
                authorizationServiceStub.Object,
                dummyLogger.Object,
                dummyDirectory.Object,
                reportingMock.Object,
                dummyRateLimiter.Object,
                availableTargetsStub.Object,
                authenticationServiceStub.Object
            );

            controller.Get(new LapRequestModel { ComputerName = @"Computer" });

            reportingMock
                .Verify(svc => svc.PerformAuditFailureActions(
                    It.IsAny<LapRequestModel>(),
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<Exception>(),
                    dummyTarget.Object,
                    It.IsAny<AuthorizationResponse>(),
                    It.IsAny<IUser>(),
                    It.IsAny<IComputer>()));
        }
    }
}