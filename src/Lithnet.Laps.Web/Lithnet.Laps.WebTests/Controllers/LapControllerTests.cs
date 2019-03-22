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
        private Mock<IComputer> dummyComputer;
        private Mock<IRateLimiter> dummyRateLimiter;
        private Mock<ITarget> dummyTarget;
        private Mock<IUser> dummyUser;
        private Mock<IAuthorizationService> dummyAuthorizationService;
        private Mock<IAvailableTargets> dummyAvailableTargets;

        private Mock<IAuthenticationService> authenticationServiceStub;

        [SetUp()]
        public void TestInitialize()
        {
            dummyLogger = new Mock<ILogger>();
            dummyComputer = new Mock<IComputer>();
            dummyRateLimiter = new Mock<IRateLimiter>();
            dummyTarget = new Mock<ITarget>();
            dummyUser = new Mock<IUser>();
            dummyAuthorizationService = new Mock<IAuthorizationService>();
            dummyAvailableTargets = new Mock<IAvailableTargets>();

            // Stub that always authenticates
            authenticationServiceStub = new Mock<IAuthenticationService>();
            authenticationServiceStub
                .Setup(svc => svc.GetLoggedInUser())
                .Returns(dummyUser.Object);
        }

        [Test()]
        public void GetPassesTargetToReportingWhenAuthorizationFails()
        {
            var directoryStub = new Mock<IDirectory>();
            var authorizationServiceStub = new Mock<IAuthorizationService>();
            var availableTargetsStub = new Mock<IAvailableTargets>();

            var reportingMock = new Mock<IReporting>();

            directoryStub.Setup(d => d.GetComputer(It.IsAny<string>()))
                .Returns(dummyComputer.Object);

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
                directoryStub.Object,
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

        [Test()]
        public void GetLogsErrorWhenComputerNotFound()
        {
            var directoryStub = new Mock<IDirectory>();
            var reportingMock = new Mock<IReporting>();

            // Computer not found.
            directoryStub.Setup(d => d.GetComputer(It.IsAny<string>()))
                .Returns((IComputer)null);

            var controller = new LapController(
                dummyAuthorizationService.Object,
                dummyLogger.Object,
                directoryStub.Object,
                reportingMock.Object,
                dummyRateLimiter.Object,
                dummyAvailableTargets.Object,
                authenticationServiceStub.Object
            );

            controller.Get(new LapRequestModel { ComputerName = @"Computer" });

            reportingMock
                .Verify(svc => svc.LogErrorEvent(
                    It.Is<int>(i => i == EventIDs.ComputerNotFound),
                    It.IsAny<string>(),
                    It.IsAny<Exception>()
                ));
        }
    }
}