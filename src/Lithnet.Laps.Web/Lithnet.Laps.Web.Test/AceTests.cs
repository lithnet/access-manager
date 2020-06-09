using Lithnet.Laps.Web.Authorization;
using Moq;
using NLog;
using NUnit.Framework;

namespace Lithnet.Laps.Web.Test
{
    public class AceTests
    {
        private Mock<ILogger> dummyLogger;

        [SetUp()]
        public void TestInitialize()
        {
            dummyLogger = new Mock<ILogger>();
        }

        // Test domain local groups in THIS domain against a computer in THIS domain
        [TestCase("idmdev1\\G-DL-1", "idmdev1\\PC1", "idmdev1\\user1")]
        [TestCase("idmdev1\\G-DL-1", "idmdev1\\PC1", "subdev1\\user1")]
        [TestCase("idmdev1\\G-DL-2", "idmdev1\\PC1", "idmdev1\\user2")]
        [TestCase("idmdev1\\G-DL-2", "idmdev1\\PC1", "subdev1\\user2")]
        [TestCase("idmdev1\\G-DL-3", "idmdev1\\PC1", "idmdev1\\user3")]
        [TestCase("idmdev1\\G-DL-3", "idmdev1\\PC1", "subdev1\\user3")]

        // Test universal groups in THIS domain against a computer in THIS domain
        [TestCase("idmdev1\\G-UG-1", "idmdev1\\PC1", "idmdev1\\user1")]
        [TestCase("idmdev1\\G-UG-1", "idmdev1\\PC1", "subdev1\\user1")]
        [TestCase("idmdev1\\G-UG-2", "idmdev1\\PC1", "idmdev1\\user2")]
        [TestCase("idmdev1\\G-UG-2", "idmdev1\\PC1", "subdev1\\user2")]
        [TestCase("idmdev1\\G-UG-3", "idmdev1\\PC1", "idmdev1\\user3")]
        [TestCase("idmdev1\\G-UG-3", "idmdev1\\PC1", "subdev1\\user3")]

        // Test universal groups in THIS domain against a computer in OTHER domain
        [TestCase("idmdev1\\G-UG-1", "subdev1\\PC1", "idmdev1\\user1")]
        [TestCase("idmdev1\\G-UG-1", "subdev1\\PC1", "subdev1\\user1")]
        [TestCase("idmdev1\\G-UG-2", "subdev1\\PC1", "idmdev1\\user2")]
        [TestCase("idmdev1\\G-UG-2", "subdev1\\PC1", "subdev1\\user2")]
        [TestCase("idmdev1\\G-UG-3", "subdev1\\PC1", "idmdev1\\user3")]
        [TestCase("idmdev1\\G-UG-3", "subdev1\\PC1", "subdev1\\user3")]

        // Test universal groups in OTHER domain against a computer in THIS domain
        [TestCase("subdev1\\G-UG-1", "idmdev1\\PC1", "idmdev1\\user1")]
        [TestCase("subdev1\\G-UG-1", "idmdev1\\PC1", "subdev1\\user1")]
        [TestCase("subdev1\\G-UG-2", "idmdev1\\PC1", "idmdev1\\user2")]
        [TestCase("subdev1\\G-UG-2", "idmdev1\\PC1", "subdev1\\user2")]
        [TestCase("subdev1\\G-UG-3", "idmdev1\\PC1", "idmdev1\\user3")]
        [TestCase("subdev1\\G-UG-3", "idmdev1\\PC1", "subdev1\\user3")]

        // Test universal groups in OTHER domain against a computer in OTHER domain
        [TestCase("subdev1\\G-UG-1", "subdev1\\PC1", "idmdev1\\user1")]
        [TestCase("subdev1\\G-UG-1", "subdev1\\PC1", "subdev1\\user1")]
        [TestCase("subdev1\\G-UG-2", "subdev1\\PC1", "idmdev1\\user2")]
        [TestCase("subdev1\\G-UG-2", "subdev1\\PC1", "subdev1\\user2")]
        [TestCase("subdev1\\G-UG-3", "subdev1\\PC1", "idmdev1\\user3")]
        [TestCase("subdev1\\G-UG-3", "subdev1\\PC1", "subdev1\\user3")]

        // Test domain local groups in OTHER domain against a computer in OTHER domain
        [TestCase("subdev1\\G-DL-1", "subdev1\\PC1", "idmdev1\\user1")]
        [TestCase("subdev1\\G-DL-1", "subdev1\\PC1", "subdev1\\user1")]
        [TestCase("subdev1\\G-DL-2", "subdev1\\PC1", "idmdev1\\user2")]
        [TestCase("subdev1\\G-DL-2", "subdev1\\PC1", "subdev1\\user2")]
        [TestCase("subdev1\\G-DL-3", "subdev1\\PC1", "idmdev1\\user3")]
        [TestCase("subdev1\\G-DL-3", "subdev1\\PC1", "subdev1\\user3")]

        public void TestAceMatch(string acePrincipal, string computer, string requestor)
        {
            Assert.IsTrue(this.IsMatch(acePrincipal, computer, requestor));
        }

        // Test domain local groups in OTHER domain against a computer in THIS domain
        [TestCase("subdev1\\G-DL-1", "idmdev1\\PC1", "idmdev1\\user1")]
        [TestCase("subdev1\\G-DL-1", "idmdev1\\PC1", "subdev1\\user1")]
        [TestCase("subdev1\\G-DL-2", "idmdev1\\PC1", "idmdev1\\user2")]
        [TestCase("subdev1\\G-DL-2", "idmdev1\\PC1", "subdev1\\user2")]
        [TestCase("subdev1\\G-DL-3", "idmdev1\\PC1", "idmdev1\\user3")]
        [TestCase("subdev1\\G-DL-3", "idmdev1\\PC1", "subdev1\\user3")]

        // Test domain local groups in THIS domain against a computer in OTHER domain
        [TestCase("idmdev1\\G-DL-1", "subdev1\\PC1", "idmdev1\\user1")]
        [TestCase("idmdev1\\G-DL-1", "subdev1\\PC1", "subdev1\\user1")]
        [TestCase("idmdev1\\G-DL-2", "subdev1\\PC1", "idmdev1\\user2")]
        [TestCase("idmdev1\\G-DL-2", "subdev1\\PC1", "subdev1\\user2")]
        [TestCase("idmdev1\\G-DL-3", "subdev1\\PC1", "idmdev1\\user3")]
        [TestCase("idmdev1\\G-DL-3", "subdev1\\PC1", "subdev1\\user3")]
        public void TestAceNotMatch(string acePrincipal, string computer, string requestor)
        {
            Assert.IsFalse(this.IsMatch(acePrincipal, computer, requestor));
        }

        private bool IsMatch(string acePrincipal, string computer, string requestor)
        {
            Mock<IAce> ace = new Mock<IAce>();
            ace.SetupGet(a => a.Name).Returns(acePrincipal);
            ace.SetupGet(x => x.Type).Returns(AceType.Allow);

            ActiveDirectory.ActiveDirectory d = new ActiveDirectory.ActiveDirectory();

            AceEvaluator evaluator = new AceEvaluator(d, dummyLogger.Object);

            return evaluator.IsMatchingAce(ace.Object, d.GetComputer(computer), d.GetUser(requestor));
        }
    }
}