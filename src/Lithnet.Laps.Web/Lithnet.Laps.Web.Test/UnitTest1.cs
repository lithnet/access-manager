using Lithnet.Laps.Web.ActiveDirectory;
using Lithnet.Laps.Web.AppSettings;
using Lithnet.Laps.Web.Authorization;
using Moq;
using NLog;
using NUnit.Framework;
using System.IO;

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
        [TestCase("subdev1\\G-DL-1", "subdev1\\PC1", "subdev1\\user1")]
        [TestCase("subdev1\\G-GG-1", "subdev1\\PC1", "subdev1\\user2")]
        [TestCase("subdev1\\G-UG-1", "subdev1\\PC1", "subdev1\\user3")]
        [TestCase("subdev1\\G-DL-1", "subdev1\\PC1", "idmdev1\\user1")]
        //[TestCase("subdev1\\G-GG-1", "subdev1\\PC1", "subdev1\\user2")]
        [TestCase("subdev1\\G-UG-1", "subdev1\\PC1", "idmdev1\\user3")]
        [TestCase("idmdev1\\G-DL-1", "idmdev1\\PC1", "idmdev1\\user1")]
        [TestCase("idmdev1\\G-GG-1", "idmdev1\\PC1", "idmdev1\\user2")]
        [TestCase("idmdev1\\G-UG-1", "idmdev1\\PC1", "idmdev1\\user3")]
        [TestCase("idmdev1\\G-DL-1", "idmdev1\\PC1", "subdev1\\user1")]
        //[TestCase("idmdev1\\G-GG-1", "idmdev1\\PC1", "subdev1\\user2")]
        [TestCase("idmdev1\\G-UG-1", "idmdev1\\PC1", "subdev1\\user3")]
        [TestCase("idmdev1\\G-DL-1", "idmdev1\\PC1", "extdev1\\user1")]
        public void Test1(string acePrincipal, string computer, string requestor)
        {
            Mock<IAce> ace = new Mock<IAce>();
            ace.SetupGet(a => a.Name).Returns(acePrincipal);
            ace.SetupGet(x => x.Type).Returns(AceType.Allow);

            ActiveDirectory.ActiveDirectory d = new ActiveDirectory.ActiveDirectory();

            AceEvaluator evaluator = new AceEvaluator(d, dummyLogger.Object);

            Assert.IsTrue(evaluator.IsMatchingAce(ace.Object, d.GetComputer(computer), d.GetUser(requestor)));
        }
    }
}