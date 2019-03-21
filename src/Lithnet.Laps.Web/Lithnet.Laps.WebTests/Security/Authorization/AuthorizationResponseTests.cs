using NUnit.Framework;
using System;

namespace Lithnet.Laps.Web.Security.Authorization.ConfigurationFile.Tests
{
    [TestFixture()]
    public class AuthorizationResponseTests
    {
        [Test()]
        public void UsersToNotifyOfAuthorizationResponseShouldNotBeNull()
        {
            // I would love to use Nullable References, but I think this is not possible for Asp.Net?
            var result = AuthorizationResponse.Authorized(null, String.Empty);
            Assert.IsNotNull(result.UsersToNotify);
        }
    }
}