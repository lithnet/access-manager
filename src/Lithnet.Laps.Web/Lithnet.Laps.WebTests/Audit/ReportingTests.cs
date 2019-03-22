using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Lithnet.Laps.Web.Mail;
using Lithnet.Laps.Web.Models;
using Lithnet.Laps.Web.Security.Authorization;
using Moq;
using NLog;

namespace Lithnet.Laps.Web.Audit.Tests
{
    [TestFixture()]
    public class ReportingTests
    {
        private Mock<ILogger> dummyLogger;
        private Mock<IComputer> dummyComputer;
        private Mock<IUser> dummyUser;
        private Mock<ITemplates> dummyTemplates;

        [SetUp()]
        public void TestInitialize()
        {
            dummyLogger = new Mock<ILogger>();
            dummyComputer = new Mock<IComputer>();
            dummyUser = new Mock<IUser>();
            dummyTemplates = new Mock<ITemplates>();
        }

        [Test()]
        public void PerformAuditFailureActionsSendsEmailToCorrectUsers()
        {
            var lapsConfigStub = new Mock<ILapsConfig>();
            var targetStub = new Mock<ITarget>();
            var mailerMock = new Mock<IMailer>();

            lapsConfigStub
                .Setup(cfg => cfg.UsersToNotify)
                .Returns(new UsersToNotify("nicole@example.com", "hugo@example.com"));
            targetStub.Setup(t => t.UsersToNotify).Returns((UsersToNotify)null);

            var reporting = new Reporting(dummyLogger.Object, lapsConfigStub.Object, mailerMock.Object, dummyTemplates.Object);

            reporting.PerformAuditFailureActions(
                new LapRequestModel {ComputerName = "SomeComputer"},
                "Something went wrong.",
                42,
                "PEBKAC",
                null,
                targetStub.Object,
                AuthorizationResponse.Authorized(
                    new UsersToNotify("mon@example.com", "mik@example.com,mak@example.com"), String.Empty
                ),
                dummyUser.Object,
                dummyComputer.Object
            );

            mailerMock.Verify(m => m.SendEmail(
                It.Is<IEnumerable<string>>(adr => adr.Contains("hugo@example.com")
                    && adr.Contains("mik@example.com") && adr.Contains("mak@example.com") && adr.Count() == 3),
                It.IsAny<string>(),
                It.IsAny<string>()
            ));
        }
    }
}