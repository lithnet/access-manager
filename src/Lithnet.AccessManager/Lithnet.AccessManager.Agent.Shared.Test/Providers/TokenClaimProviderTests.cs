using NUnit.Framework;
using System.Linq;
using Lithnet.AccessManager.Api.Shared;
using Moq;

namespace Lithnet.AccessManager.Agent.Providers.Test
{
    [TestFixture()]
    public class TokenClaimProviderTests
    {

        [SetUp()]
        public void TestInitialize()
        {
        }
      
        [Test()]
        public void GetClaimsForAmsAuthTest()
        {
            var settingsMock = new Mock<IAgentSettings>();
            settingsMock.SetupGet(t => t.AuthenticationMode).Returns(AgentAuthenticationMode.Ams);

            TokenClaimProvider provider = new TokenClaimProvider(settingsMock.Object);

            var result = provider.GetClaims().ToList();

            Assert.IsTrue(result.All(t => t.Type != AmsClaimNames.AadDeviceId));
            Assert.IsTrue(result.All(t => t.Type != AmsClaimNames.AadTenantId));
            Assert.IsTrue(result.Any(t => t.Type == AmsClaimNames.AuthMode && t.Value == AgentAuthenticationMode.Ams.ToString()));
        }
    }
}