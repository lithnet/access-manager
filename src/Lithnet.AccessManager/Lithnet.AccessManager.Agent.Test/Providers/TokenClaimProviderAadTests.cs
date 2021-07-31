using NUnit.Framework;
using System.Linq;
using Lithnet.AccessManager.Api.Shared;
using Moq;

namespace Lithnet.AccessManager.Agent.Providers.Test
{
    [TestFixture()]
    public class TokenClaimProviderTests
    {
        private Mock<IAadJoinInformationProvider> aadProviderMock;
        private IAadJoinInformationProvider aadProvider;

        [SetUp()]
        public void TestInitialize()
        {
            this.aadProviderMock = new Mock<IAadJoinInformationProvider>();
            this.aadProviderMock.SetupGet(t => t.TenantId).Returns("ABC");
            this.aadProviderMock.SetupGet(t => t.DeviceId).Returns("DEF");
            this.aadProvider = this.aadProviderMock.Object;
        }

        [Test()]
        public void GetClaimsForAadAuthTest()
        {
            var settingsMock = new Mock<IAgentSettings>();
            settingsMock.SetupGet(t => t.AuthenticationMode).Returns(AgentAuthenticationMode.Aad);

            TokenClaimProviderAad provider = new TokenClaimProviderAad(settingsMock.Object, this.aadProvider);

            var result = provider.GetClaims().ToList();

            Assert.IsTrue(result.Any(t => t.Type == AmsClaimNames.AadDeviceId && t.Value == "DEF"));
            Assert.IsTrue(result.Any(t => t.Type == AmsClaimNames.AadTenantId && t.Value == "ABC"));
            Assert.IsTrue(result.Any(t => t.Type == AmsClaimNames.AuthMode && t.Value == AgentAuthenticationMode.Aad.ToString()));
        }

        [Test()]
        public void GetClaimsForAmsAuthTest()
        {
            var settingsMock = new Mock<IAgentSettings>();
            settingsMock.SetupGet(t => t.AuthenticationMode).Returns(AgentAuthenticationMode.Ams);

            TokenClaimProviderAad provider = new TokenClaimProviderAad(settingsMock.Object, this.aadProvider);

            var result = provider.GetClaims().ToList();

            Assert.IsTrue(result.Count == 0);
        }
    }
}