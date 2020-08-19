using System.Collections.Generic;
using Lithnet.AccessManager.Server.Authorization;
using Lithnet.AccessManager.Server.Configuration;
using NUnit.Framework;

namespace Lithnet.AccessManager.Server.Test
{
    public class AuthorizationContextDomainDetailsTests
    {
        private IDirectory directory;

        [SetUp]
        public void TestInitialize()
        {
            directory = new ActiveDirectory();
        }

        [Test]
        public void TestGetServerOrder()
        {
            IUser user = directory.GetUser("IDMDEV1\\user1");
            string dnsDomain = this.directory.GetDomainNameDnsFromSid(user.Sid);

            AuthorizationContextDomainDetails d = new AuthorizationContextDomainDetails(user.Sid.AccountDomainSid, dnsDomain, directory);
            d.Mapping = new AuthorizationServerMapping
            {
                Domain = dnsDomain,
                DisableLocalFallback = true,
                Servers = new List<AuthorizationServer>
                {
                    new AuthorizationServer
                    {
                        Name = "madeup.local",
                        Type = AuthorizationServerType.Default
                    },
                    new AuthorizationServer
                    {
                        Name = "madeup2.local",
                        Type = AuthorizationServerType.Default
                    }
                }
            };
            
            Assert.AreEqual("madeup.local", d.GetServer(false).Name); // Get the first entry
            Assert.AreEqual("madeup.local", d.GetServer(false).Name); // Call GetServer again and we should get the same entry
            Assert.AreEqual("madeup2.local", d.GetServer(true).Name); // Ask for the next entry
            Assert.AreEqual("madeup2.local", d.GetServer(false).Name); // Make sure we still get the same entry
            Assert.AreEqual("madeup.local", d.GetServer(true).Name); // Make sure we return to the start
        }

        [Test]
        public void TestExternalDomainDetails()
        {
            IUser user = directory.GetUser("EXTDEV1\\user1");
            string dnsDomain = this.directory.GetDomainNameDnsFromSid(user.Sid);

            AuthorizationContextDomainDetails d = new AuthorizationContextDomainDetails(user.Sid.AccountDomainSid, dnsDomain, directory);
            Assert.IsFalse(d.IsInCurrentForest);
            Assert.IsTrue(d.IsRemoteOneWayTrust);
        }

        [Test]
        public void TestThisDomainDetails()
        {
            IUser user = directory.GetUser("IDMDEV1\\user1");
            string dnsDomain = this.directory.GetDomainNameDnsFromSid(user.Sid);

            AuthorizationContextDomainDetails d = new AuthorizationContextDomainDetails(user.Sid.AccountDomainSid, dnsDomain, directory);
            Assert.IsTrue(d.IsInCurrentForest);
            Assert.IsFalse(d.IsRemoteOneWayTrust);
        }

        [Test]
        public void TestChildDomainDetails()
        {
            IUser user = directory.GetUser("SUBDEV1\\user1");
            string dnsDomain = this.directory.GetDomainNameDnsFromSid(user.Sid);

            AuthorizationContextDomainDetails d = new AuthorizationContextDomainDetails(user.Sid.AccountDomainSid, dnsDomain, directory);
            Assert.IsTrue(d.IsInCurrentForest);
            Assert.IsFalse(d.IsRemoteOneWayTrust);
        }

        [Test]
        public void TestGetDC()
        {
            IUser user = directory.GetUser("IDMDEV1\\user1");
            string dnsDomain = this.directory.GetDomainNameDnsFromSid(user.Sid);
            string dc = this.directory.GetDomainControllerForDomain(dnsDomain);

            AuthorizationContextDomainDetails d = new AuthorizationContextDomainDetails(user.Sid.AccountDomainSid, dnsDomain, directory);
            d.Mapping = new AuthorizationServerMapping
            {
                Domain = dnsDomain,
            };

            Assert.AreEqual(dc, d.GetServer(false).Name); // Get the first entry
        }
    }
}