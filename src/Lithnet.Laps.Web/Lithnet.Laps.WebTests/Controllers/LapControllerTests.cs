using NUnit.Framework;
using System;
using Lithnet.Laps.Web.Audit;
using Lithnet.Laps.Web.Config;
using Lithnet.Laps.Web.Models;
using Lithnet.Laps.Web.Security.Authentication;
using Lithnet.Laps.Web.Security.Authorization;
using Moq;
using NLog;
using Lithnet.Laps.Web.AppSettings;
using Lithnet.Laps.Web.JsonTargets;

namespace Lithnet.Laps.Web.Controllers.Tests
{
    [TestFixture()]
    public class LapControllerTests
    {
        private Mock<ILogger> dummyLogger;
        private Mock<IComputer> dummyComputer;
        private Mock<IRateLimiter> dummyRateLimiter;
        private Mock<IUser> dummyUser;
        private Mock<IAuthorizationService> dummyAuthorizationService;
        private Mock<IDirectory> dummyDirectory;
        private Mock<IUserInterfaceSettings> dummyUiSettings;

        private Mock<IAuthenticationService> authenticationServiceStub;

        [SetUp()]
        public void TestInitialize()
        {
            dummyLogger = new Mock<ILogger>();
            dummyComputer = new Mock<IComputer>();
            dummyRateLimiter = new Mock<IRateLimiter>();
            dummyUser = new Mock<IUser>();
            dummyAuthorizationService = new Mock<IAuthorizationService>();
            dummyDirectory = new Mock<IDirectory>();
            dummyUiSettings = new Mock<IUserInterfaceSettings>();

            dummyDirectory.Setup(d => d.GetUser(It.IsAny<string>()))
                .Returns(dummyUser.Object);

            // Stub that always authenticates
            authenticationServiceStub = new Mock<IAuthenticationService>();
            authenticationServiceStub
                .Setup(svc => svc.GetLoggedInUser(dummyDirectory.Object))
                .Returns(dummyUser.Object);
        }

        [Test()]
        public void GetPassesTargetToReportingWhenAuthorizationFails()
        {
            var directoryStub = new Mock<IDirectory>();
            var authorizationServiceStub = new Mock<IAuthorizationService>();

            var reportingMock = new Mock<IReporting>();

            directoryStub.Setup(d => d.GetComputer(It.IsAny<string>()))
                .Returns(dummyComputer.Object);

            authorizationServiceStub
                .Setup(svc => svc.GetAuthorizationResponse(It.IsAny<IUser>(), It.IsAny<IComputer>()))
                .Returns(new AuthorizationResponse() { ResponseCode = AuthorizationResponseCode.NoMatchingRuleForUser });


            // Hmmm... this controller has a lot of dependencies.
            var controller = new LapController(
                authorizationServiceStub.Object,
                dummyLogger.Object,
                directoryStub.Object,
                reportingMock.Object,
                dummyRateLimiter.Object,
                authenticationServiceStub.Object, dummyUiSettings.Object
            );

            controller.Get(new LapRequestModel { ComputerName = @"Computer" });

            reportingMock
                .Verify(svc => svc.PerformAuditFailureActions(
                    It.IsAny<LapRequestModel>(),
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<Exception>(),
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
                authenticationServiceStub.Object, dummyUiSettings.Object
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