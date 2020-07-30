using System.Security.Principal;
using Lithnet.AccessManager.Server.Authorization;
using Lithnet.AccessManager.Server.Configuration;
using Lithnet.AccessManager.Web.Internal;
using Lithnet.Security.Authorization;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Lithnet.AccessManager.Server.Test
{
    public class PowershellSecurityDescriptorGeneratorTests
    {
        private IDirectory directory;

        private PowerShellSecurityDescriptorGenerator generator;

        private ILogger<PowerShellSecurityDescriptorGenerator> psLogger;

        private ILogger<CachedPowerShellSessionProvider> sessionLogger;

        [SetUp()]
        public void TestInitialize()
        {
            directory = new ActiveDirectory();
            psLogger = Global.LogFactory.CreateLogger<PowerShellSecurityDescriptorGenerator>();
            sessionLogger = Global.LogFactory.CreateLogger<CachedPowerShellSessionProvider>();

            var provider = new TestPathProvider();
            var sessionp = new CachedPowerShellSessionProvider(provider, sessionLogger);
            generator = new PowerShellSecurityDescriptorGenerator(psLogger, sessionp);
        }

        [Test]
        public void TestScriptGrantLapsJit()
        {
            IUser user = directory.GetUser(WindowsIdentity.GetCurrent().User);

            var sd = generator.GenerateSecurityDescriptor(user, null, "AuthZTestGrantLapsJit.ps1", 30);

            AuthorizationContext context = new AuthorizationContext(user.Sid);
            Assert.IsTrue(context.AccessCheck(sd, (int)AccessMask.Jit));
            Assert.IsTrue(context.AccessCheck(sd, (int)AccessMask.Laps));
            Assert.IsFalse(context.AccessCheck(sd, (int)AccessMask.LapsHistory));
        }

        [Test]
        public void TestScriptDenyLapsJitGrantLapsHistory()
        {
            IUser user = directory.GetUser(WindowsIdentity.GetCurrent().User);

            var sd = generator.GenerateSecurityDescriptor(user, null, "AuthZTestDenyLapsJitGrantLapsHistory.ps1", 30);

            AuthorizationContext context = new AuthorizationContext(user.Sid);
            Assert.IsFalse(context.AccessCheck(sd, (int)AccessMask.Jit));
            Assert.IsFalse(context.AccessCheck(sd, (int)AccessMask.Laps));
            Assert.IsTrue(context.AccessCheck(sd, (int)AccessMask.LapsHistory));
        }

        [TestCase(AccessMask.Laps | AccessMask.LapsHistory | AccessMask.Jit, AccessMask.None)]
        [TestCase(AccessMask.Laps | AccessMask.LapsHistory | AccessMask.Jit, AccessMask.Laps)]
        [TestCase(AccessMask.None, AccessMask.Laps)]
        [TestCase(AccessMask.Laps | AccessMask.LapsHistory | AccessMask.Jit, AccessMask.LapsHistory)]
        [TestCase(AccessMask.None, AccessMask.LapsHistory)]
        [TestCase(AccessMask.Laps | AccessMask.LapsHistory | AccessMask.Jit, AccessMask.Jit)]
        [TestCase(AccessMask.None, AccessMask.Jit)]
        [TestCase(AccessMask.None, AccessMask.None)]
        public void TestSd(AccessMask allowedAccess, AccessMask deniedAccess)
        {
            SecurityIdentifier user = WindowsIdentity.GetCurrent().User;

            PowerShellAuthorizationResponse response = this.AccessMaskToPowerShellAuthorizationResponse(allowedAccess, deniedAccess);
            var sd = generator.GenerateSecurityDescriptor(user, response);

            AuthorizationContext context = new AuthorizationContext(user);

            Assert.AreEqual(response.IsJitAllowed && !response.IsJitDenied, context.AccessCheck(sd, (int)AccessMask.Jit));
            Assert.AreEqual(response.IsLocalAdminPasswordAllowed && !response.IsLocalAdminPasswordDenied, context.AccessCheck(sd, (int)AccessMask.Laps));
            Assert.AreEqual(response.IsLocalAdminPasswordHistoryAllowed && !response.IsLocalAdminPasswordHistoryDenied, context.AccessCheck(sd, (int)AccessMask.LapsHistory));
        }

        [TestCase(AccessMask.Laps, AccessMask.None, true)]
        [TestCase(AccessMask.Jit, AccessMask.None, false)]
        [TestCase(AccessMask.LapsHistory, AccessMask.None, false)]
        [TestCase(AccessMask.Laps, AccessMask.Laps, false)]
        [TestCase(AccessMask.Laps, AccessMask.LapsHistory, true)]
        [TestCase(AccessMask.Laps, AccessMask.Jit, true)]
        [TestCase(AccessMask.None, AccessMask.Laps, false)]
        [TestCase(AccessMask.None, AccessMask.None, false)]
        public void TestLapsSecurityDescriptor(AccessMask allowedAccess, AccessMask deniedAccess, bool expectedResult)
        {
            SecurityIdentifier user = WindowsIdentity.GetCurrent().User;

            PowerShellAuthorizationResponse response = this.AccessMaskToPowerShellAuthorizationResponse(allowedAccess, deniedAccess);
            var sd = generator.GenerateSecurityDescriptor(user, response);

            AuthorizationContext context = new AuthorizationContext(user);

            Assert.AreEqual(expectedResult, context.AccessCheck(sd, (int)AccessMask.Laps));
        }

        [TestCase(AccessMask.LapsHistory, AccessMask.None, true)]
        [TestCase(AccessMask.Jit, AccessMask.None, false)]
        [TestCase(AccessMask.Laps, AccessMask.None, false)]
        [TestCase(AccessMask.LapsHistory, AccessMask.LapsHistory, false)]
        [TestCase(AccessMask.LapsHistory, AccessMask.Laps, true)]
        [TestCase(AccessMask.LapsHistory, AccessMask.Jit, true)]
        [TestCase(AccessMask.None, AccessMask.LapsHistory, false)]
        [TestCase(AccessMask.None, AccessMask.None, false)]
        public void TestLapsHistorySecurityDescriptor(AccessMask allowedAccess, AccessMask deniedAccess, bool expectedResult)
        {
            SecurityIdentifier user = WindowsIdentity.GetCurrent().User;

            PowerShellAuthorizationResponse response = this.AccessMaskToPowerShellAuthorizationResponse(allowedAccess, deniedAccess);
            var sd = generator.GenerateSecurityDescriptor(user, response);

            AuthorizationContext context = new AuthorizationContext(user);

            Assert.AreEqual(expectedResult, context.AccessCheck(sd, (int)AccessMask.LapsHistory));
        }

        [TestCase(AccessMask.Jit, AccessMask.None, true)]
        [TestCase(AccessMask.LapsHistory, AccessMask.None, false)]
        [TestCase(AccessMask.Laps, AccessMask.None, false)]
        [TestCase(AccessMask.Jit, AccessMask.Jit, false)]
        [TestCase(AccessMask.Jit, AccessMask.Laps, true)]
        [TestCase(AccessMask.Jit, AccessMask.LapsHistory, true)]
        [TestCase(AccessMask.None, AccessMask.Jit, false)]
        [TestCase(AccessMask.None, AccessMask.None, false)]
        public void TestJitSecurityDescriptor(AccessMask allowedAccess, AccessMask deniedAccess, bool expectedResult)
        {
            SecurityIdentifier user = WindowsIdentity.GetCurrent().User;

            PowerShellAuthorizationResponse response = this.AccessMaskToPowerShellAuthorizationResponse(allowedAccess, deniedAccess);
            var sd = generator.GenerateSecurityDescriptor(user, response);

            AuthorizationContext context = new AuthorizationContext(user);

            Assert.AreEqual(expectedResult, context.AccessCheck(sd, (int)AccessMask.Jit));
        }


        private PowerShellAuthorizationResponse AccessMaskToPowerShellAuthorizationResponse(AccessMask allowedAccessMask, AccessMask deniedAccessMask)
        {
            PowerShellAuthorizationResponse response = new PowerShellAuthorizationResponse
            {
                IsLocalAdminPasswordAllowed = allowedAccessMask.HasFlag(AccessMask.Laps),
                IsLocalAdminPasswordHistoryAllowed = allowedAccessMask.HasFlag(AccessMask.LapsHistory),
                IsJitAllowed = allowedAccessMask.HasFlag(AccessMask.Jit),
                IsLocalAdminPasswordDenied = deniedAccessMask.HasFlag(AccessMask.Laps),
                IsLocalAdminPasswordHistoryDenied = deniedAccessMask.HasFlag(AccessMask.LapsHistory),
                IsJitDenied = deniedAccessMask.HasFlag(AccessMask.Jit)
            };

            return response;
        }
    }
}