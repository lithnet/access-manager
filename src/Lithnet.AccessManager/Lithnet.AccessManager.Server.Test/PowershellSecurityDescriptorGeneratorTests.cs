using System.Security.Principal;
using Lithnet.AccessManager.Server.Authorization;
using Lithnet.AccessManager.Server.Configuration;
using Lithnet.AccessManager.Service.Internal;
using Lithnet.Security.Authorization;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using C = Lithnet.AccessManager.Test.TestEnvironmentConstants;

namespace Lithnet.AccessManager.Server.Test
{
    public class PowershellSecurityDescriptorGeneratorTests
    {
        private IActiveDirectory directory;

        private IDiscoveryServices discoveryServices;

        private PowerShellSecurityDescriptorGenerator generator;

        private ILogger<PowerShellSecurityDescriptorGenerator> psLogger;

        private ILogger<CachedPowerShellSessionProvider> sessionLogger;

        [SetUp()]
        public void TestInitialize()
        {
            this.discoveryServices = new DiscoveryServices(Mock.Of<ILogger<DiscoveryServices>>());
            directory = new ActiveDirectory(discoveryServices);
            psLogger = Global.LogFactory.CreateLogger<PowerShellSecurityDescriptorGenerator>();
            sessionLogger = Global.LogFactory.CreateLogger<CachedPowerShellSessionProvider>();

            var provider = new TestPathProvider();
            var sessionp = new CachedPowerShellSessionProvider(provider, sessionLogger);
            generator = new PowerShellSecurityDescriptorGenerator(psLogger, sessionp);
        }

        [Test]
        public void TestScriptGrantLapsJit()
        {
            IActiveDirectoryUser user = directory.GetUser(WindowsIdentity.GetCurrent().User);
            IActiveDirectoryComputer computer = directory.GetComputer(C.DEV_PC1); 
            var sd = generator.GenerateSecurityDescriptor(user, computer, "AuthZTestGrantLapsJit.ps1", 30);

            using AuthorizationContext context = new AuthorizationContext(user.Sid);
            Assert.IsTrue(context.AccessCheck(sd, (int)AccessMask.Jit));
            Assert.IsTrue(context.AccessCheck(sd, (int)AccessMask.LocalAdminPassword));
            Assert.IsFalse(context.AccessCheck(sd, (int)AccessMask.LocalAdminPasswordHistory));
        }

        [Test]
        public void TestScriptDenyLapsJitGrantLapsHistory()
        {
            IActiveDirectoryUser user = directory.GetUser(WindowsIdentity.GetCurrent().User);
            IActiveDirectoryComputer computer = directory.GetComputer(C.DEV_PC1);
            var sd = generator.GenerateSecurityDescriptor(user, computer, "AuthZTestDenyLapsJitGrantLapsHistory.ps1", 30);

            using AuthorizationContext context = new AuthorizationContext(user.Sid);
            Assert.IsFalse(context.AccessCheck(sd, (int)AccessMask.Jit));
            Assert.IsFalse(context.AccessCheck(sd, (int)AccessMask.LocalAdminPassword));
            Assert.IsTrue(context.AccessCheck(sd, (int)AccessMask.LocalAdminPasswordHistory));
        }

        [TestCase(AccessMask.LocalAdminPassword | AccessMask.LocalAdminPasswordHistory | AccessMask.Jit, AccessMask.None)]
        [TestCase(AccessMask.LocalAdminPassword | AccessMask.LocalAdminPasswordHistory | AccessMask.Jit, AccessMask.LocalAdminPassword)]
        [TestCase(AccessMask.None, AccessMask.LocalAdminPassword)]
        [TestCase(AccessMask.LocalAdminPassword | AccessMask.LocalAdminPasswordHistory | AccessMask.Jit, AccessMask.LocalAdminPasswordHistory)]
        [TestCase(AccessMask.None, AccessMask.LocalAdminPasswordHistory)]
        [TestCase(AccessMask.LocalAdminPassword | AccessMask.LocalAdminPasswordHistory | AccessMask.Jit, AccessMask.Jit)]
        [TestCase(AccessMask.None, AccessMask.Jit)]
        [TestCase(AccessMask.None, AccessMask.None)]
        public void TestSd(AccessMask allowedAccess, AccessMask deniedAccess)
        {
            SecurityIdentifier user = WindowsIdentity.GetCurrent().User;

            PowerShellAuthorizationResponse response = this.AccessMaskToPowerShellAuthorizationResponse(allowedAccess, deniedAccess);
            var sd = generator.GenerateSecurityDescriptor(user, response);

            using AuthorizationContext context = new AuthorizationContext(user);

            Assert.AreEqual(response.IsJitAllowed && !response.IsJitDenied, context.AccessCheck(sd, (int)AccessMask.Jit));
            Assert.AreEqual(response.IsLocalAdminPasswordAllowed && !response.IsLocalAdminPasswordDenied, context.AccessCheck(sd, (int)AccessMask.LocalAdminPassword));
            Assert.AreEqual(response.IsLocalAdminPasswordHistoryAllowed && !response.IsLocalAdminPasswordHistoryDenied, context.AccessCheck(sd, (int)AccessMask.LocalAdminPasswordHistory));
        }

        [TestCase(AccessMask.LocalAdminPassword, AccessMask.None, true)]
        [TestCase(AccessMask.Jit, AccessMask.None, false)]
        [TestCase(AccessMask.LocalAdminPasswordHistory, AccessMask.None, false)]
        [TestCase(AccessMask.LocalAdminPassword, AccessMask.LocalAdminPassword, false)]
        [TestCase(AccessMask.LocalAdminPassword, AccessMask.LocalAdminPasswordHistory, true)]
        [TestCase(AccessMask.LocalAdminPassword, AccessMask.Jit, true)]
        [TestCase(AccessMask.None, AccessMask.LocalAdminPassword, false)]
        [TestCase(AccessMask.None, AccessMask.None, false)]
        public void TestLapsSecurityDescriptor(AccessMask allowedAccess, AccessMask deniedAccess, bool expectedResult)
        {
            SecurityIdentifier user = WindowsIdentity.GetCurrent().User;

            PowerShellAuthorizationResponse response = this.AccessMaskToPowerShellAuthorizationResponse(allowedAccess, deniedAccess);
            var sd = generator.GenerateSecurityDescriptor(user, response);

            using AuthorizationContext context = new AuthorizationContext(user);

            Assert.AreEqual(expectedResult, context.AccessCheck(sd, (int)AccessMask.LocalAdminPassword));
        }

        [TestCase(AccessMask.LocalAdminPasswordHistory, AccessMask.None, true)]
        [TestCase(AccessMask.Jit, AccessMask.None, false)]
        [TestCase(AccessMask.LocalAdminPassword, AccessMask.None, false)]
        [TestCase(AccessMask.LocalAdminPasswordHistory, AccessMask.LocalAdminPasswordHistory, false)]
        [TestCase(AccessMask.LocalAdminPasswordHistory, AccessMask.LocalAdminPassword, true)]
        [TestCase(AccessMask.LocalAdminPasswordHistory, AccessMask.Jit, true)]
        [TestCase(AccessMask.None, AccessMask.LocalAdminPasswordHistory, false)]
        [TestCase(AccessMask.None, AccessMask.None, false)]
        public void TestLapsHistorySecurityDescriptor(AccessMask allowedAccess, AccessMask deniedAccess, bool expectedResult)
        {
            SecurityIdentifier user = WindowsIdentity.GetCurrent().User;

            PowerShellAuthorizationResponse response = this.AccessMaskToPowerShellAuthorizationResponse(allowedAccess, deniedAccess);
            var sd = generator.GenerateSecurityDescriptor(user, response);

            using AuthorizationContext context = new AuthorizationContext(user);

            Assert.AreEqual(expectedResult, context.AccessCheck(sd, (int)AccessMask.LocalAdminPasswordHistory));
        }

        [TestCase(AccessMask.Jit, AccessMask.None, true)]
        [TestCase(AccessMask.LocalAdminPasswordHistory, AccessMask.None, false)]
        [TestCase(AccessMask.LocalAdminPassword, AccessMask.None, false)]
        [TestCase(AccessMask.Jit, AccessMask.Jit, false)]
        [TestCase(AccessMask.Jit, AccessMask.LocalAdminPassword, true)]
        [TestCase(AccessMask.Jit, AccessMask.LocalAdminPasswordHistory, true)]
        [TestCase(AccessMask.None, AccessMask.Jit, false)]
        [TestCase(AccessMask.None, AccessMask.None, false)]
        public void TestJitSecurityDescriptor(AccessMask allowedAccess, AccessMask deniedAccess, bool expectedResult)
        {
            SecurityIdentifier user = WindowsIdentity.GetCurrent().User;

            PowerShellAuthorizationResponse response = this.AccessMaskToPowerShellAuthorizationResponse(allowedAccess, deniedAccess);
            var sd = generator.GenerateSecurityDescriptor(user, response);

            using AuthorizationContext context = new AuthorizationContext(user);

            Assert.AreEqual(expectedResult, context.AccessCheck(sd, (int)AccessMask.Jit));
        }


        private PowerShellAuthorizationResponse AccessMaskToPowerShellAuthorizationResponse(AccessMask allowedAccessMask, AccessMask deniedAccessMask)
        {
            PowerShellAuthorizationResponse response = new PowerShellAuthorizationResponse
            {
                IsLocalAdminPasswordAllowed = allowedAccessMask.HasFlag(AccessMask.LocalAdminPassword),
                IsLocalAdminPasswordHistoryAllowed = allowedAccessMask.HasFlag(AccessMask.LocalAdminPasswordHistory),
                IsJitAllowed = allowedAccessMask.HasFlag(AccessMask.Jit),
                IsLocalAdminPasswordDenied = deniedAccessMask.HasFlag(AccessMask.LocalAdminPassword),
                IsLocalAdminPasswordHistoryDenied = deniedAccessMask.HasFlag(AccessMask.LocalAdminPasswordHistory),
                IsJitDenied = deniedAccessMask.HasFlag(AccessMask.Jit)
            };

            return response;
        }
    }
}