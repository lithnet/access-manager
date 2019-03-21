using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Lithnet.Laps.Web.Security.Authorization.ConfigurationFile.Tests
{
    [TestClass()]
    public class AuthorizationResponseTests
    {
        [TestMethod()]
        public void UsersToNotifyOfAuthorizationResponseShouldNotBeNull()
        {
            // I would love to use Nullable References, but I think this is not possible for Asp.Net?
            var result = AuthorizationResponse.Authorized(null, String.Empty);
            Assert.IsNotNull(result.UsersToNotify);
        }
    }
}