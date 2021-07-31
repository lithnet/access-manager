using Lithnet.AccessManager.Api.Shared;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Agent.Providers.Test
{
    [TestFixture()]
    public class RegistrationProviderTests
    {
        private Mock<IAgentCheckInProvider> agentCheckinProviderMock;
        private Mock<IAmsApiHttpClient> amsApiHttpClientMock;
        private Mock<IAuthenticationCertificateProvider> authCertProviderMock;
        private Mock<IAgentSettings> agentSettingsMock;
        private Mock<IClientAssertionProvider> clientAssertionProviderMock;
        private ILogger<RegistrationProvider> logger;


        [SetUp()]
        public void TestInitialize()
        {
            this.logger = Global.LogFactory.CreateLogger<RegistrationProvider>();
            this.agentCheckinProviderMock = new Mock<IAgentCheckInProvider>();
            this.amsApiHttpClientMock = new Mock<IAmsApiHttpClient>();
            this.authCertProviderMock = new Mock<IAuthenticationCertificateProvider>();
            this.clientAssertionProviderMock = new Mock<IClientAssertionProvider>();
            this.agentSettingsMock = new Mock<IAgentSettings>();
        }

        [Test()]
        public void CanRegisterAgentOnNotRegisteredTest()
        {
            this.agentSettingsMock.SetupGet(t => t.RegistrationKey).Returns("ABC");
            this.agentSettingsMock.SetupGet(t => t.RegistrationState).Returns(RegistrationState.NotRegistered);

            var provider = this.BuildProvider();
            Assert.IsTrue(provider.CanRegisterAgent());
        }

        [Test()]
        public void CanRegisterAgentOnApprovedTest()
        {
            this.agentSettingsMock.SetupGet(t => t.RegistrationKey).Returns("ABC");
            this.agentSettingsMock.SetupGet(t => t.RegistrationState).Returns(RegistrationState.Approved);

            var provider = this.BuildProvider();
            Assert.IsFalse(provider.CanRegisterAgent());
        }

        [Test()]
        public void CanRegisterAgentOnPendingTest()
        {
            this.agentSettingsMock.SetupGet(t => t.RegistrationKey).Returns("ABC");
            this.agentSettingsMock.SetupGet(t => t.RegistrationState).Returns(RegistrationState.Pending);

            var provider = this.BuildProvider();
            Assert.IsFalse(provider.CanRegisterAgent());
        }

        [Test()]
        public void CanRegisterAgentOnRejectedWithKeyTest()
        {
            this.agentSettingsMock.SetupGet(t => t.RegistrationKey).Returns("ABC");
            this.agentSettingsMock.SetupGet(t => t.RegistrationState).Returns(RegistrationState.Rejected);

            var provider = this.BuildProvider();
            Assert.IsTrue(provider.CanRegisterAgent());
        }

        [Test()]
        public void CanRegisterAgentOnRejectedWithoutKeyTest()
        {
            this.agentSettingsMock.SetupGet(t => t.RegistrationKey).Returns(string.Empty);
            this.agentSettingsMock.SetupGet(t => t.RegistrationState).Returns(RegistrationState.Rejected);

            var provider = this.BuildProvider();
            Assert.IsFalse(provider.CanRegisterAgent());
        }

        [Test()]
        public void CanRegisterAgentOnMissingKeyTest()
        {
            this.agentSettingsMock.SetupGet(t => t.RegistrationKey).Returns(string.Empty);
            this.agentSettingsMock.SetupGet(t => t.RegistrationState).Returns(RegistrationState.NotRegistered);

            var provider = this.BuildProvider();
            Assert.IsFalse(provider.CanRegisterAgent());
        }

        [Test()]
        public void RegisterSecondaryCredentialsForAmsAuthTest()
        {
            this.agentSettingsMock.SetupGet(t => t.AuthenticationMode).Returns(Api.Shared.AgentAuthenticationMode.Ams);

            var provider = this.BuildProvider();
            Assert.ThrowsAsync<NotSupportedException>(async () => await provider.RegisterSecondaryCredentials());
        }

        [Test()]
        public void RegisterSecondaryCredentialsForIwaAuthTest()
        {
            this.agentSettingsMock.SetupGet(t => t.AuthenticationMode).Returns(Api.Shared.AgentAuthenticationMode.Iwa);

            var provider = this.BuildProvider();
            Assert.ThrowsAsync<NotSupportedException>(async () => await provider.RegisterSecondaryCredentials());
        }

        [Test()]
        public void RegisterSecondaryCredentialsForNoneAuthTest()
        {
            this.agentSettingsMock.SetupGet(t => t.AuthenticationMode).Returns(Api.Shared.AgentAuthenticationMode.None);

            var provider = this.BuildProvider();
            Assert.ThrowsAsync<NotSupportedException>(async () => await provider.RegisterSecondaryCredentials());
        }

        [Test()]
        public async Task RegisterSecondaryCredentialsForAadAuthTestAsync()
        {
            this.agentSettingsMock.SetupGet(t => t.AuthenticationMode).Returns(Api.Shared.AgentAuthenticationMode.Aad);
            this.authCertProviderMock.Setup(t => t.GetOrCreateAgentCertificate()).ReturnsAsync(Global.TestCertificate);
            this.clientAssertionProviderMock.Setup(t => t.BuildAssertion(It.IsAny<X509Certificate2>(), It.IsAny<string>())).ReturnsAsync(new ClientAssertion());
            this.amsApiHttpClientMock.Setup(t => t.BuildUrl(It.IsAny<string>())).Returns("https://unittest");
            this.amsApiHttpClientMock.Setup(t => t.RegisterSecondaryCredentialsAsync(It.IsAny<ClientAssertion>()));

            var provider = this.BuildProvider();
            await provider.RegisterSecondaryCredentials();

            this.amsApiHttpClientMock.Verify(t => t.RegisterSecondaryCredentialsAsync(It.IsAny<ClientAssertion>()), Times.Once);
            this.agentSettingsMock.VerifySet(t => t.HasRegisteredSecondaryCredentials = true, Times.Once);
        }

        [Test()]
        public async Task RegisterAgentApprovedTest()
        {
            this.SetupAgentRegistrationMocks();

            string client = Guid.NewGuid().ToString();

            this.amsApiHttpClientMock.Setup(t => t.RegisterAgentAsync(It.IsAny<ClientAssertion>())).Returns(Task.FromResult(new RegistrationResponse()
            {
                ApprovalState = ApprovalState.Approved,
                ClientId = client
            }));

            var provider = this.BuildProvider();
            await provider.RegisterAgent();

            this.amsApiHttpClientMock.Verify(t => t.RegisterAgentAsync(It.IsAny<ClientAssertion>()), Times.Once);
            this.agentSettingsMock.VerifySet(t => t.RegistrationKey = null, Times.Once);
            this.agentSettingsMock.VerifySet(t => t.ClientId = client, Times.Once);
            this.agentSettingsMock.VerifySet(t => t.RegistrationState = RegistrationState.Approved, Times.Once);
        }

        [Test()]
        public async Task RegisterAgentPendingTest()
        {
            this.SetupAgentRegistrationMocks();

            string client = Guid.NewGuid().ToString();

            this.amsApiHttpClientMock.Setup(t => t.RegisterAgentAsync(It.IsAny<ClientAssertion>())).Returns(Task.FromResult(new RegistrationResponse()
            {
                ApprovalState = ApprovalState.Pending,
                ClientId = client
            }));

            var provider = this.BuildProvider();
            await provider.RegisterAgent();

            this.amsApiHttpClientMock.Verify(t => t.RegisterAgentAsync(It.IsAny<ClientAssertion>()), Times.Once);
            this.agentSettingsMock.VerifySet(t => t.RegistrationKey = null, Times.Once);
            this.agentSettingsMock.VerifySet(t => t.ClientId = client, Times.Once);
            this.agentSettingsMock.VerifySet(t => t.RegistrationState = RegistrationState.Pending, Times.Once);
        }

        [Test()]
        public async Task RegisterAgentRejectedTest()
        {
            this.SetupAgentRegistrationMocks();

            string client = Guid.NewGuid().ToString();

            this.amsApiHttpClientMock.Setup(t => t.RegisterAgentAsync(It.IsAny<ClientAssertion>())).Returns(Task.FromResult(new RegistrationResponse()
            {
                ApprovalState = ApprovalState.Rejected,
                ClientId = client
            }));

            var provider = this.BuildProvider();
            await provider.RegisterAgent();

            this.amsApiHttpClientMock.Verify(t => t.RegisterAgentAsync(It.IsAny<ClientAssertion>()), Times.Once);
            this.agentSettingsMock.VerifySet(t => t.RegistrationKey = null, Times.Once);
            this.agentSettingsMock.VerifySet(t => t.ClientId = client, Times.Once);
            this.agentSettingsMock.VerifySet(t => t.RegistrationState = RegistrationState.Rejected, Times.Once);
        }

        [Test()]
        public async Task RegisterAgentInvalidKeyTest()
        {
            this.SetupAgentRegistrationMocks();

            string client = Guid.NewGuid().ToString();

            this.amsApiHttpClientMock.Setup(t => t.RegisterAgentAsync(It.IsAny<ClientAssertion>())).Throws(new ApiException("test") { ApiErrorCode = ApiConstants.InvalidRegistrationKey });

            var provider = this.BuildProvider();
            await provider.RegisterAgent();

            this.amsApiHttpClientMock.Verify(t => t.RegisterAgentAsync(It.IsAny<ClientAssertion>()), Times.Once);
            this.agentSettingsMock.VerifySet(t => t.RegistrationKey = null, Times.Once);
            this.agentSettingsMock.VerifySet(t => t.ClientId = null, Times.Never);
            this.agentSettingsMock.VerifySet(t => t.RegistrationState = RegistrationState.Rejected, Times.Once);
        }

        private void SetupAgentRegistrationMocks()
        {
            this.agentSettingsMock.SetupGet(t => t.AuthenticationMode).Returns(AgentAuthenticationMode.Ams);
            this.agentSettingsMock.SetupGet(t => t.RegistrationKey).Returns("ABC");
            this.authCertProviderMock.Setup(t => t.GetOrCreateAgentCertificate()).ReturnsAsync(Global.TestCertificate);
            this.agentCheckinProviderMock.Setup(t => t.GenerateCheckInData()).ReturnsAsync(new AgentCheckIn());
            this.clientAssertionProviderMock.Setup(t => t.BuildAssertion(It.IsAny<X509Certificate2>(), It.IsAny<string>())).ReturnsAsync(new ClientAssertion());
            this.amsApiHttpClientMock.Setup(t => t.BuildUrl(It.IsAny<string>())).Returns("https://unittest");
        }

        private RegistrationProvider BuildProvider()
        {
            return new RegistrationProvider(
                this.amsApiHttpClientMock.Object,
                this.agentSettingsMock.Object,
                this.authCertProviderMock.Object,
                this.logger,
                this.agentCheckinProviderMock.Object,
                this.clientAssertionProviderMock.Object);
        }
    }
}